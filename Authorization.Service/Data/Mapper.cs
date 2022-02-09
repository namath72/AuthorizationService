using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Authorization.Service.Models;

namespace Authorization.Service.Data
{
	public static class Mapper
	{
		public static UserResponseModel ToUserResponse ( ApplicationUser user)
		{
			return new UserResponseModel
			{
				Name = user.Name,
				LastName = user.LastName,
				Email = user.Email,
				Avatar = user.Avatar,
				LastLogin = user.LastLogin,
				Loggedin = user.Loggedin
			};
		}

		public static List<UserResponseModel> ToUsersResponse (List<ApplicationUser> users)
		{
			var model = new List<UserResponseModel>();
			foreach( var user in users)
			{
				model.Add(new UserResponseModel
				{
					Name = user.Name,
					LastName = user.LastName,
					Email = user.Email,
					Avatar = user.Avatar,
					LastLogin = user.LastLogin,
					Loggedin = user.Loggedin
				});
			}
			return model;
		}
	}
}
