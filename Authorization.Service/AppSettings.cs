namespace Authorization.Service
{
	public class AppSettings
	{
		public string SigningKey { get; set; }
		public string Issuer { get; set; }
		public string Audience { get; set; }
		public int AccessTokenExpireTime { get; set; }
		public int RefreshTokenExpireTime { get; set; }
	}
}
