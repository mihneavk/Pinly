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

        // --- DbSet-uri ---
        public DbSet<Reaction> Reactions { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Pin> Pins { get; set; }
        
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMessage> GroupMessages { get; set; }
        public DbSet<GroupMembership> GroupMemberships { get; set; }

        // --- Configurare Relații (DOAR O SINGURĂ DATĂ) ---
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 
            // CONFIGURAREA RELAȚIEI N:M (Reaction)
            // 
            
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
            
            // 
            // CONFIGURAREA RELAȚIEI N:M (GroupMembership)
            // 
        
            // 1. Definirea cheii primare compuse (GroupId + ApplicationUserId)
            builder.Entity<GroupMembership>()
                .HasKey(gm => new { gm.GroupId, gm.ApplicationUserId });

            // 2. Definirea relației cu Group
            builder.Entity<GroupMembership>()
                .HasOne(gm => gm.Group) 
                .WithMany(g => g.Memberships) 
                .HasForeignKey(gm => gm.GroupId) 
                .OnDelete(DeleteBehavior.Cascade); 

            // 3. Definirea relației cu ApplicationUser
            builder.Entity<GroupMembership>()
                .HasOne(gm => gm.ApplicationUser) 
                .WithMany(u => u.Memberships) 
                .HasForeignKey(gm => gm.ApplicationUserId) 
                .OnDelete(DeleteBehavior.Restrict); 
            
            // 
            // CONFIGURAREA RELAȚIEI 1:N (Moderator)
            // 
            
        
            // Configurarea explicită a relației User -> Group (Moderator)
            builder.Entity<Group>()
                .HasOne(g => g.Moderator)
                .WithMany(u => u.ModeratedGroups) // Folosim colecția adăugată în ApplicationUser
                .HasForeignKey(g => g.ModeratorId)
                .OnDelete(DeleteBehavior.Restrict);
            
        }
    }
}