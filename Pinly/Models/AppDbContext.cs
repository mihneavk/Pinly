using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pinly.Models;

namespace Pinly.Models
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Reaction> Reactions { get; set; }
        
        public DbSet<Pin> Pins { get; set; }
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // -------------------------------------------------------------------
            // CONFIGURAREA RELAȚIEI N:M (Reaction)
            // -------------------------------------------------------------------
            
            builder.Entity<Reaction>()
                .HasKey(r => new { r.PinId, r.ApplicationUserId });

            builder.Entity<Reaction>()
                .HasOne(r => r.Pin) 
                .WithMany(p => p.Reactions) 
                .HasForeignKey(r => r.PinId) 
                .OnDelete(DeleteBehavior.Restrict); 

            builder.Entity<Reaction>()
                .HasOne(r => r.ApplicationUser) 
                .WithMany() 
                .HasForeignKey(r => r.ApplicationUserId) 
                .OnDelete(DeleteBehavior.Restrict); 
        }
    }
    
}