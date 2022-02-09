
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Authorization.Service.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base (options)
        {

        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

			builder.Entity<ApplicationUser>().ToTable("Users").Property(p => p.Id).HasColumnName("UserId");
			builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
			builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
			builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
			builder.Entity<ApplicationRole>().ToTable("Roles").Property(p =>p.Id).HasColumnName("RoleId");
			builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaim");
			builder.Entity<IdentityUserToken<string>>().ToTable("UserToken");
		}
    }
}
