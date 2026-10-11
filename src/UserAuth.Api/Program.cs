using System.Security.Claims;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UserAuth.Api.Config;
using UserAuth.Api.Data;
using UserAuth.Api.Models;
using UserAuth.Api.Repositories;
using UserAuth.Api.Services;
using UserAuth.Api.Services.Interfaces;
using UserAuth.Api.Validators;

var contentRoot = Directory.GetCurrentDirectory();
Directory.CreateDirectory(Path.Combine(contentRoot, "wwwroot", "uploads", "profile-pictures"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection("JwtSettings"));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000", "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)));
});

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
{
    throw new InvalidOperationException("JwtSettings:Secret must be configured.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IDoctorPatientContactService, DoctorPatientContactService>();
builder.Services.AddHttpClient<UserAuth.Api.Clients.AppointmentAccessClient>(client => {
    if (Uri.TryCreate(builder.Configuration["AppointmentIntegration:BaseUrl"], UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
        client.BaseAddress = new Uri(uri.ToString().TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("FrontendPolicy");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();

// Deactivating a user must end their active session immediately: JWTs are stateless and
// stay valid until they expire, so re-check IsActive against the DB on every authenticated
// request rather than only at login (GCN-24 Scenario 5).
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var subject = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (Guid.TryParse(subject, out var userId))
        {
            var userRepository = context.RequestServices.GetRequiredService<IUserRepository>();
            var user = await userRepository.GetByIdAsync(userId, context.RequestAborted);

            if (user is null || !user.IsActive)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "This account is inactive. Please sign in again." });
                return;
            }
        }
    }

    await next();
});

app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    await SeedAdminAsync(dbContext, passwordHasher, app.Configuration);
}

app.Run();

static async Task SeedAdminAsync(AppDbContext dbContext, IPasswordHasher<User> passwordHasher, IConfiguration configuration)
{
    var adminEmail = configuration["SeedAdmin:Email"];
    var adminPassword = configuration["SeedAdmin:Password"];
    var adminFullName = configuration["SeedAdmin:FullName"];

    if (string.IsNullOrWhiteSpace(adminEmail)
        || string.IsNullOrWhiteSpace(adminPassword)
        || string.IsNullOrWhiteSpace(adminFullName))
    {
        return;
    }

    var exists = await dbContext.Users.AnyAsync(u => u.Email.ToLower() == adminEmail.ToLower());
    if (exists)
    {
        return;
    }

    var adminUser = new User
    {
        Id = Guid.NewGuid(),
        FullName = adminFullName,
        Email = adminEmail,
        MobileNumber = "0770000000",
        LandPhoneNumber = "0110000000",
        Address = "CareNova HQ",
        DateOfBirth = new DateTime(1990, 1, 1),
        Gender = "Other",
        Role = RoleNames.Admin,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, adminPassword);
    dbContext.Users.Add(adminUser);
    await dbContext.SaveChangesAsync();
}

public partial class Program
{
}
