using System;
using Microsoft.AspNetCore.Identity;

namespace Authorization.Service.Data
{
    public class ApplicationUser: IdentityUser
    {
		public string Name { get; set; }

		public string LastName { get; set; }

		public string RefreshToken { get; set; }

		public DateTime LastLogin { get; set; }

		public bool Loggedin { get; set; }

		public byte[] Avatar { get; set; }
	}
}
