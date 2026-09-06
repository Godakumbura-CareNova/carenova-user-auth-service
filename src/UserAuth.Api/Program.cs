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

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "server=localhost;port=3306;database=carenova_user_auth;uid=root;pwd=Password123!";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("CareNovaUserAuthDb"));

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("FrontendPolicy");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    await SeedAdminAsync(dbContext, passwordHasher, app.Configuration);
}

app.Run();

static async Task SeedAdminAsync(AppDbContext dbContext, IPasswordHasher<User> passwordHasher, IConfiguration configuration)
{
    var adminEmail = configuration["SeedAdmin:Email"] ?? "admin@carenova.org";
    var adminPassword = configuration["SeedAdmin:Password"] ?? "Admin@12345";
    var adminFullName = configuration["SeedAdmin:FullName"] ?? "System Administrator";

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
