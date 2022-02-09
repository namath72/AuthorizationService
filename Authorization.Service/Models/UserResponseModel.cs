using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Authorization.Service.Models
{
	public class UserResponseModel
	{
		public string Name { get; set; }

		public string LastName { get; set; }

		public DateTime LastLogin { get; set; }

		public bool Loggedin { get; set; }

		public byte[] Avatar { get; set; }

		public string Email { get; set; }
	}
}
