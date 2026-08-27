using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UserAuth.Api.Config;
using UserAuth.Api.Data;
using UserAuth.Api.Models;
using UserAuth.Api.Repositories;
using UserAuth.Api.Services;
using UserAuth.Api.Services.Interfaces;
using UserAuth.Api.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection("JwtSettings"));

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings are not configured.");
if (string.IsNullOrWhiteSpace(jwtSettings.Secret)
    || string.IsNullOrWhiteSpace(jwtSettings.Issuer)
    || string.IsNullOrWhiteSpace(jwtSettings.Audience)
    || jwtSettings.ExpiryInMinutes <= 0)
{
    throw new InvalidOperationException("JWT settings must include a secret, issuer, audience, and positive expiry.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "server=localhost;port=3306;database=carenova_user_auth;uid=root;pwd=Password123!";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)));
        return;
    }

    options.UseInMemoryDatabase("CareNovaUserAuthDb");
});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<User>, Microsoft.AspNetCore.Identity.PasswordHasher<User>>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    // Local-only seed accounts so every teammate has the same test logins for
    // every role without manually registering + promoting each one via SQL.
    // Never runs outside Development — known email/password pairs must never
    // exist in a deployed environment.
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.IPasswordHasher<User>>();

        var seedUsers = new[]
        {
            new { Email = "test-patient@carenova.dev", FullName = "Test Patient", Role = "Patient", Password = "Patient@2026" },
            new { Email = "test-doctor@carenova.dev", FullName = "Test Doctor", Role = "Doctor", Password = "Doctor@2026" },
            new { Email = "test-pharmacy@carenova.dev", FullName = "Test Pharmacy", Role = "PharmacyStaff", Password = "Pharmacy@2026" },
            new { Email = "admin@carenova.dev", FullName = "System Admin", Role = "Admin", Password = "Admin@2026" },
        };

        foreach (var seed in seedUsers)
        {
            if (db.Users.Any(u => u.Email == seed.Email))
            {
                continue;
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = seed.FullName,
                Email = seed.Email,
                MobileNumber = "0000000000",
                LandPhoneNumber = "0000000000",
                Address = "Godakumbura Pharmacy & Channelling Centre, Welimada",
                DateOfBirth = new DateTime(2000, 1, 1),
                Gender = "Other",
                Role = seed.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            user.PasswordHash = hasher.HashPassword(user, seed.Password);

            db.Users.Add(user);
        }

        db.SaveChanges();
    }
}

app.UseCors("FrontendPolicy");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
}
