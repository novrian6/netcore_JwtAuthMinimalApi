using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JwtAuthMinimalApi.Models;
using JwtAuthMinimalApi.Data;
using JwtAuthMinimalApi.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Use SQLite for database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=shop.db"));

// JWT secret key - at least 32 chars
var jwtKey = "mysupersecurekeythatlongenough123!";
var key = Encoding.ASCII.GetBytes(jwtKey);

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };

    // Custom response for unauthorized access
    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            // Skip the default behavior
            context.HandleResponse();

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            return context.Response.WriteAsync("{\"error\": \"Unauthorized. Please provide a valid token.\"}");
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbInitializer.Initialize(db);
}

app.UseAuthentication();
app.UseAuthorization();

// Public route
app.MapGet("/", () => Results.Ok("Welcome to the public API homepage."));

// Login endpoint
app.MapPost("/login", async (AppDbContext db, LoginRequest login) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u =>
        u.Username == login.Username && u.Password == login.Password);

    if (user == null) return Results.Unauthorized();

    var tokenHandler = new JwtSecurityTokenHandler();
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role ?? "User"),
            new Claim("FullName", user.FullName ?? ""),
            new Claim("Email", user.Email ?? "")
        }),
        Expires = DateTime.UtcNow.AddHours(1),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    var tokenString = tokenHandler.WriteToken(token);

    return Results.Ok(new { Token = tokenString });
});

// Dashboard - Any authenticated user
app.MapGet("/dashboard", (ClaimsPrincipal user) =>
{
    var name = user.Identity?.Name ?? "Unknown";
    var email = user.FindFirst("Email")?.Value ?? "N/A";
    var role = user.FindFirst(ClaimTypes.Role)?.Value ?? "N/A";

    return Results.Ok(new
    {
        Page = "Dashboard",
        Message = $"Welcome {name}",
        Email = email,
        Role = role
    });
}).RequireAuthorization();

// Admin - Only for users with Role = Admin
app.MapGet("/admin", (ClaimsPrincipal user) =>
{
    var name = user.Identity?.Name ?? "Unknown";

    return Results.Ok(new
    {
        Page = "Admin Panel",
        Message = $"Hello Admin {name}"
    });
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

app.Run();