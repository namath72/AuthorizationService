using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Authorization.Service.Services
{
	public class TokenService : ITokenServices
	{
		private readonly AppSettings _appSettings;

		public TokenService(AppSettings appSettings)
		{
			_appSettings = appSettings;
		}

		public string GenerateAccessToken(IEnumerable<Claim> claims)
		{
			var key = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.SigningKey)), SecurityAlgorithms.HmacSha256);

			var jwtToken = new JwtSecurityToken(
				issuer: _appSettings.Issuer,
				audience: _appSettings.Audience,
				expires: DateTime.Now.AddMinutes(_appSettings.AccessTokenExpireTime),
				claims: claims,
				signingCredentials: key
				);
			return new JwtSecurityTokenHandler().WriteToken(jwtToken);
		}

		public string GenerateRefreshToken()
		{
			var key = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.SigningKey)), SecurityAlgorithms.HmacSha256);

			var jwtToken = new JwtSecurityToken(
				issuer: _appSettings.Issuer,
				audience: _appSettings.Audience,
				expires: DateTime.Now.AddMinutes(_appSettings.RefreshTokenExpireTime),
				signingCredentials: key
				);
			return new JwtSecurityTokenHandler().WriteToken(jwtToken);
		}

		public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
		{
			var tokenValidationParameters = new TokenValidationParameters
			{
				ValidateAudience = true, 
				ValidateIssuer = true,
				ValidateIssuerSigningKey = true,
				ValidIssuer = _appSettings.Issuer,
				ValidAudience = _appSettings.Audience,
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.SigningKey)),
				ValidateLifetime = false 
			};

			var tokenHandler = new JwtSecurityTokenHandler();
			var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
			if (!(securityToken is JwtSecurityToken jwtSecurityToken) || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
				throw new SecurityTokenException("Invalid token");

			return principal;
		}

		public bool ValidateRefreshToken(string refreshToken)
		{
			var isValid = false;
			var tokenValidationParameters = new TokenValidationParameters
			{
				ValidateAudience = true,
				ValidateIssuer = true,
				ValidateIssuerSigningKey = true,
				ValidIssuer = _appSettings.Issuer,
				ValidAudience = _appSettings.Audience,
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.SigningKey)),
				ValidateLifetime = false
			};
			try
			{
				var tokenHandler = new JwtSecurityTokenHandler();
				var principal = tokenHandler.ValidateToken(refreshToken, tokenValidationParameters, out SecurityToken securityToken);
				isValid = true;
			}
			catch (SecurityTokenExpiredException)
			{
				return isValid;
			}
			return isValid;
		}
	}
}
