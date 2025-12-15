using Microsoft.EntityFrameworkCore;

namespace Pinly.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Aici definim tabelele:
        public DbSet<User> Users { get; set; }
        public DbSet<Pin> Pins { get; set; }
    }
}