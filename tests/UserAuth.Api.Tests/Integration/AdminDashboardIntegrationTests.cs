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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserAuth.Api.Data;
using UserAuth.Api.Models;

namespace UserAuth.Api.Tests.Integration;

public class AdminDashboardIntegrationTests
{
    [Fact]
    public async Task EmptyDatabaseHasZeroCounts()
    {
        using var factory = new Factory(); using var scope = factory.Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<UserAuth.Api.Services.AdminUserDashboardService>().GetAsync(default);
        Assert.Equal(0, result.TotalUsers); Assert.Equal(0, result.Patients); Assert.Equal(0, result.PharmacyStaff); Assert.Equal(0, result.Doctors);
    }

    [Fact]
    public async Task CountsIncludeAliasesInactiveAccountsAndOtherRoles()
    {
        using var factory = new Factory();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roles = new[] { "Patient", "User", "PharmacyStaff", "Pharmacy Staff", "Doctor", "Admin", "Historical" };
            db.Users.AddRange(roles.Select((role, i) => new User { Id = Guid.NewGuid(), Role = role, IsActive = i % 2 == 0,
                Email = $"private-{i}@test.invalid", FullName = "Private Name", PasswordHash = "private-hash" }));
            await db.SaveChangesAsync();
        }
        using var client = factory.Client("Admin");
        var response = await client.GetAsync("/api/admin/dashboard/users");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(7, data.GetProperty("totalUsers").GetInt32());
        Assert.Equal(2, data.GetProperty("patients").GetInt32());
        Assert.Equal(2, data.GetProperty("pharmacyStaff").GetInt32());
        Assert.Equal(1, data.GetProperty("doctors").GetInt32());
        Assert.Equal(new[] { "doctors", "generatedAtUtc", "patients", "pharmacyStaff", "totalUsers" }, data.EnumerateObject().Select(x => x.Name).Order().ToArray());
        Assert.DoesNotContain("private", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(null, 401)] [InlineData("Patient", 403)] [InlineData("User", 403)]
    [InlineData("PharmacyStaff", 403)] [InlineData("Pharmacy Staff", 403)] [InlineData("Doctor", 403)] [InlineData("Admin", 200)]
    public async Task OnlyAdminCanReadSummary(string? role, int status)
    {
        using var factory = new Factory(); using var client = factory.Client(role);
        var response = await client.GetAsync("/api/admin/dashboard/users?role=Admin");
        Assert.Equal(status, (int)response.StatusCode);
        if (status == 200)
        {
            var data = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, data.GetProperty("totalUsers").GetInt32()); // Authenticated account exists.
            foreach (var key in new[] { "patients", "pharmacyStaff", "doctors" }) Assert.Equal(0, data.GetProperty(key).GetInt32());
        }
    }

    private sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly string database = Guid.NewGuid().ToString();
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?> {
                ["JwtSettings:Secret"] = "test-only-secret-at-least-thirty-two-characters", ["SeedAdmin:Email"] = "",
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=unused;User=test;Password=test"
            }));
            return base.CreateHost(builder);
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services => {
                services.RemoveAll<AppDbContext>(); services.RemoveAll<DbContextOptions<AppDbContext>>();
                foreach (var item in services.Where(x => x.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration") == true).ToArray()) services.Remove(item);
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(database));
                services.AddAuthentication(o => { o.DefaultAuthenticateScheme = "AdminTests"; o.DefaultChallengeScheme = "AdminTests"; o.DefaultForbidScheme = "AdminTests"; })
                    .AddScheme<AuthenticationSchemeOptions, Identity>("AdminTests", _ => { });
            });
        }
        public HttpClient Client(string? role)
        {
            var client = CreateClient();
            if (role != null)
            {
                using var scope = Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var account = db.Users.FirstOrDefault(u => u.IsActive);
                if (account == null) { account = new User { Id = Guid.NewGuid(), Role = "Admin", Email = "admin@test.invalid" }; db.Users.Add(account); db.SaveChanges(); }
                client.DefaultRequestHeaders.Add("X-Test-Role", role);
                client.DefaultRequestHeaders.Add("X-Test-Subject", account.Id.ToString());
            }
            return client;
        }
    }
    private sealed class Identity(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(
            Request.Headers.TryGetValue("X-Test-Role", out var role)
                ? AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier, Request.Headers["X-Test-Subject"].ToString()), new Claim(ClaimTypes.Role, role.ToString()) }, "AdminTests")), "AdminTests"))
                : AuthenticateResult.NoResult());
    }
}
