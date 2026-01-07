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
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMessage> GroupMessages { get; set; }
        public DbSet<GroupMembership> GroupMemberships { get; set; }
        public DbSet<Follow> Follows { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Configurare Follow (Restrict pe ambele parti)
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

            // 2. Configurare Reaction (Fix shadow state + Restrict)
            builder.Entity<Reaction>()
                .HasKey(r => new { r.PinId, r.ApplicationUserId });

            builder.Entity<Reaction>()
                .HasOne(r => r.Pin)
                .WithMany(p => p.Reactions)
                .HasForeignKey(r => r.PinId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reaction>()
                .HasOne(r => r.ApplicationUser)
                .WithMany(u => u.Reactions) // Legatura explicita cu User
                .HasForeignKey(r => r.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Configurare GroupMembership
            builder.Entity<GroupMembership>()
                .HasKey(gm => new { gm.GroupId, gm.ApplicationUserId });

            builder.Entity<GroupMembership>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.Memberships)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GroupMembership>()
                .HasOne(gm => gm.ApplicationUser)
                .WithMany(u => u.Memberships)
                .HasForeignKey(gm => gm.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 4. Configurare Grupuri (Moderator)
            builder.Entity<Group>()
                .HasOne(g => g.Moderator)
                .WithMany(u => u.ModeratedGroups)
                .HasForeignKey(g => g.ModeratorId)
                .OnDelete(DeleteBehavior.Restrict);

            // 5. Configurare Comment
            builder.Entity<Comment>().ToTable("Comment");

            builder.Entity<Comment>()
                .HasOne(c => c.ApplicationUser)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict); // Important: opreste stergerea in cascada
        }
    }
}