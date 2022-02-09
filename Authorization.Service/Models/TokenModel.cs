using System;

namespace Authorization.Service.Models
{
	public class TokenModel
	{
		public string Id { get; set; }
		public string Username { get; set; }
		public string Token { get; set; }
		public DateTime Expires { get; set; }
		public string RefreshToken { get; set; }
	}
}
