using System.Collections.Generic;
using System.Security.Claims;

namespace Authorization.Service.Services
{
	public interface ITokenServices
	{
		string GenerateAccessToken(IEnumerable<Claim> claims);
		string GenerateRefreshToken();
		ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
		bool ValidateRefreshToken(string refreshToken);
	}
}
