using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using UserAuth.Api.Data;
using UserAuth.Api.Models;

namespace UserAuth.Api.Tests.Integration;

public class DoctorPatientContactsIntegrationTests
{
    static readonly Guid Doctor = Guid.NewGuid(), Patient = Guid.NewGuid(), Stranger = Guid.NewGuid();

    [Fact]
    public async Task OnlyAssignedPatientsMinimalContactsAreReturned()
    {
        using var factory = new ContactFactory(); await factory.Seed(); using var client = factory.Client("Doctor");
        var response = await client.PostAsJsonAsync("/api/doctor/patient-contacts", new { patientIds = new[] { Patient, Stranger } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var contact = Assert.Single((await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
        Assert.Equal(Patient, contact.GetProperty("patientId").GetGuid()); Assert.Equal("Assigned Patient", contact.GetProperty("fullName").GetString()); Assert.Equal("0771234567", contact.GetProperty("mobileNumber").GetString());
        Assert.Equal(new[] { "fullName", "mobileNumber", "patientId" }, contact.EnumerateObject().Select(x => x.Name).Order().ToArray());
        Assert.Equal("Bearer isolated-doctor-token", factory.ForwardedToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/users")).StatusCode);
    }

    [Theory]
    [InlineData(null, 401)] [InlineData("Patient", 403)] [InlineData("Admin", 403)] [InlineData("PharmacyStaff", 403)]
    public async Task ContactsAreDoctorOnly(string? role, int code)
    {
        using var factory = new ContactFactory(); await factory.Seed(); using var client = factory.Client(role);
        Assert.Equal(code, (int)(await client.PostAsJsonAsync("/api/doctor/patient-contacts", new { patientIds = new[] { Patient } })).StatusCode);
    }

    [Theory]
    [InlineData("outage")] [InlineData("invalid")] [InlineData("unexpected")] [InlineData("empty204")]
    public async Task InvalidOwnershipConfirmationFailsClosed(string failure)
    {
        using var factory = new ContactFactory { Failure = failure }; await factory.Seed(); using var client = factory.Client("Doctor");
        var response = await client.PostAsJsonAsync("/api/doctor/patient-contacts", new { patientIds = new[] { Patient } });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.DoesNotContain("Assigned Patient", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task InvalidBatchesAndInactiveDoctorAreDenied()
    {
        using var factory = new ContactFactory(); await factory.Seed(); using var client = factory.Client("Doctor");
        foreach (var ids in new[] { Array.Empty<Guid>(), new[] { Guid.Empty }, new[] { Patient, Patient }, Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToArray() })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/doctor/patient-contacts", new { patientIds = ids })).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var doctor = await db.Users.FindAsync(Doctor); doctor!.IsActive = false; await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/doctor/patient-contacts", new { patientIds = new[] { Patient } })).StatusCode);
    }

    sealed class ContactFactory : WebApplicationFactory<Program>
    {
        readonly string database = Guid.NewGuid().ToString();
        public string? Failure { get; init; }
        public string? ForwardedToken { get; set; }
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?> {
                ["JwtSettings:Secret"] = "test-only-secret-at-least-thirty-two-characters", ["SeedAdmin:Email"] = "",
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=unused;User=test;Password=test"
            }));
            return base.CreateHost(builder);
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string,string?> {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=unused;User=test;Password=test",
                ["JwtSettings:Secret"] = "test-only-secret-at-least-thirty-two-characters", ["SeedAdmin:Email"] = "", ["AppointmentIntegration:BaseUrl"] = "https://appointments.test/"
            }));
            builder.ConfigureServices(services => {
                services.RemoveAll<AppDbContext>(); services.RemoveAll<DbContextOptions<AppDbContext>>();
                foreach (var descriptor in services.Where(x => x.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration") == true).ToArray()) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(database));
                services.ConfigureAll<HttpClientFactoryOptions>(options => options.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = new OwnershipHandler(this)));
                services.AddAuthentication(options => { options.DefaultAuthenticateScheme = "ContactTests"; options.DefaultChallengeScheme = "ContactTests"; options.DefaultForbidScheme = "ContactTests"; }).AddScheme<AuthenticationSchemeOptions, TestIdentity>("ContactTests", _ => {});
            });
        }
        public async Task Seed()
        {
            using var scope = Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            db.Users.AddRange(new User { Id = Doctor, FullName = "Doctor", Role = "Doctor", Email = "doctor@test.invalid" }, new User { Id = Patient, FullName = "Assigned Patient", MobileNumber = "0771234567", Email = "private@test.invalid", Address = "Private address", PasswordHash = "private-hash" }, new User { Id = Stranger, FullName = "Other Patient", Email = "other@test.invalid" });
            await db.SaveChangesAsync();
        }
        public HttpClient Client(string? role)
        {
            var client = CreateClient();
            if (role is not null) { client.DefaultRequestHeaders.Add("X-Test-Role", role); client.DefaultRequestHeaders.Authorization = new("Bearer", "isolated-doctor-token"); }
            return client;
        }
    }
    sealed class OwnershipHandler(ContactFactory factory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            factory.ForwardedToken = request.Headers.Authorization?.ToString();
            Assert.Equal("/api/doctor/appointments/patient-access", request.RequestUri!.AbsolutePath);
            return Task.FromResult<HttpResponseMessage>(factory.Failure switch {
                "outage" => new(HttpStatusCode.ServiceUnavailable), "empty204" => new(HttpStatusCode.NoContent),
                "invalid" => new(HttpStatusCode.OK) { Content = new StringContent("not json") },
                "unexpected" => new(HttpStatusCode.OK) { Content = JsonContent.Create(new { patientIds = new[] { Stranger } }) },
                _ => new(HttpStatusCode.OK) { Content = JsonContent.Create(new { patientIds = new[] { Patient } }) }
            });
        }
    }
    sealed class TestIdentity(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(!Request.Headers.TryGetValue("X-Test-Role", out var role) ? AuthenticateResult.NoResult() : AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Doctor.ToString()), new Claim(ClaimTypes.Role, role.ToString()) }, "ContactTests")), "ContactTests")));
    }
}
