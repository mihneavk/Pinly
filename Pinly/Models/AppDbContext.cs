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

        public DbSet<Pin> Pins { get; set; }
        public DbSet<Reaction> Reactions { get; set; }

        public DbSet<Follow> Follows { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Follow>()
                .HasKey(f => new { f.FollowerId, f.FolloweeId });

            builder.Entity<Follow>()
                .HasOne(f => f.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Follow>()
                .HasOne(f => f.Followee)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FolloweeId)
                .OnDelete(DeleteBehavior.Restrict);

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