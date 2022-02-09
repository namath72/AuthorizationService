using Authorization.Service.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Authorization.Service.Data
{
	public class SeedDatabase
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            context.Database.Migrate();
			if (!context.Roles.Any())
			{
				await roleManager.CreateAsync(new ApplicationRole { Name = "Admin", Description="Administrator of the system" });
				await roleManager.CreateAsync(new ApplicationRole { Name = "Teacher", Description="Teacher" });
				await roleManager.CreateAsync(new ApplicationRole { Name = "Student", Description = "Student" });
			}
            if (!context.Users.Any())
            {
                var user = new ApplicationUser
                {
					Name = "Admin",
					LastName = "User",
                    UserName = "Administrator",
                    Email = "Admin@email.xxx",
                    SecurityStamp = Guid.NewGuid().ToString(),
					Avatar = AvatarGenerator.Generate("Admin","User"),
					RefreshToken = null
                };
                await userManager.CreateAsync(user, "Password@123");
            }
			var newUser = await userManager.FindByNameAsync("Administrator");
			if (!await userManager.IsInRoleAsync(newUser, "Admin") || !await userManager.IsInRoleAsync(newUser,"Teacher"))
			{
				await userManager.AddToRoleAsync(newUser, "Admin");
				await userManager.AddToRoleAsync(newUser, "Teacher");
			}
			if (!(await userManager.GetClaimsAsync(newUser)).Any(c => c.Type == ClaimTypes.Role))
			{
				foreach (var role in await userManager.GetRolesAsync(newUser))
				{
					await userManager.AddClaimAsync(newUser, new Claim(ClaimTypes.Role, role));
				}
			}
			var usersClaims = new[]
	{
						new Claim(ClaimTypes.Name, newUser.UserName),
						new Claim(ClaimTypes.NameIdentifier, newUser.Id.ToString())
					};
			 await userManager.AddClaimsAsync(newUser, usersClaims);
		}
    }
}
