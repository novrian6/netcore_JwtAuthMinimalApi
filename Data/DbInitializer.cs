using JwtAuthMinimalApi.Models;

namespace JwtAuthMinimalApi.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext db)
    {
        db.Database.EnsureCreated();

        if (!db.Users.Any())
        {
            db.Users.AddRange(
                new User { Username = "admin", Password = "admin", FullName = "Admin User", Email = "admin@site.com", Role = "Admin" },
                new User { Username = "john", Password = "1234", FullName = "John Doe", Email = "john@site.com", Role = "User" }
            );
            db.SaveChanges();
        }
    }
}