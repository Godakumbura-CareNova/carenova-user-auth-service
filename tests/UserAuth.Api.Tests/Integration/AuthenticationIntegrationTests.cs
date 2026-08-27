using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UserAuth.Api.Tests.Integration;

public class AuthenticationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthenticationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("JwtSettings:Secret", "a-secure-integration-test-signing-key");
            builder.UseSetting("JwtSettings:Issuer", "CareNova.UserAuth");
            builder.UseSetting("JwtSettings:Audience", "CareNova.Client");
            builder.UseSetting("JwtSettings:ExpiryInMinutes", "60");
        });
    }

    [Fact]
    public async Task Profile_WithoutTokenIsRejected_AndWithLoginTokenIsAllowed()
    {
        using var client = _factory.CreateClient();
        var registration = new
        {
            fullName = "Integration User",
            email = $"integration-{Guid.NewGuid():N}@example.com",
            password = "Test@123",
            confirmPassword = "Test@123",
            mobileNumber = "+94771234567",
            landPhoneNumber = "+94112233445",
            address = "No. 1, Colombo",
            dateOfBirth = new DateTime(1990, 1, 1),
            gender = "Male"
        };

        var unauthenticatedResponse = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticatedResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var invalidTokenResponse = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, invalidTokenResponse.StatusCode);
        client.DefaultRequestHeaders.Authorization = null;

        var registrationResponse = await client.PostAsJsonAsync("/api/auth/register", registration);
        Assert.Equal(HttpStatusCode.Created, registrationResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            registration.email,
            registration.password
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        Assert.NotNull(login?.Token);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        var authenticatedResponse = await client.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.OK, authenticatedResponse.StatusCode);
    }

    private sealed class LoginResult
    {
        public string Token { get; set; } = string.Empty;
    }
}