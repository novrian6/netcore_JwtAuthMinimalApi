using Microsoft.EntityFrameworkCore;
using JwtAuthMinimalApi.Models;

namespace JwtAuthMinimalApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
}