using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pinly.Models;

namespace Pinly.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new AppDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>()))
            {
                if (context.Roles.Any()) return;

                context.Roles.AddRange(
                    new IdentityRole { Id = "role_admin", Name = "Admin", NormalizedName = "ADMIN" },
                    new IdentityRole { Id = "role_user", Name = "User", NormalizedName = "USER" },
                    new IdentityRole { Id = "role_guest", Name = "Guest", NormalizedName = "GUEST" }
                );

                var hasher = new PasswordHasher<ApplicationUser>();

                context.Users.AddRange(
                    new ApplicationUser
                    {
                        Id = "user_admin",
                        UserName = "admin@test.com",
                        NormalizedUserName = "ADMIN@TEST.COM",
                        Email = "admin@test.com",
                        NormalizedEmail = "ADMIN@TEST.COM",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(null, "Admin1!")
                    }
                );

                context.UserRoles.AddRange(
                    new IdentityUserRole<string>
                    {
                        RoleId = "role_admin",
                        UserId = "user_admin"
                    }
                );

                context.SaveChanges();
            }
        }
    }
}