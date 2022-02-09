using System;
using System.Threading.Tasks;
using Authorization.Service.Data;
using Authorization.Service.Models;
using Authorization.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Authorization.Service.Controllers
{
    [Route("api/v{v:apiVersion}/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly ITokenServices _tokenService;
        private readonly AppSettings _appSettings;
        private readonly UserManager<ApplicationUser> _userManager;

        public TokenController(UserManager<ApplicationUser> userManager, IOptions<AppSettings> appSettings)
        {
            _userManager = userManager;
            _appSettings = appSettings.Value;
            _tokenService = new TokenService(_appSettings);
        }

        [HttpPost]
        [Route("refresh")]
        public async Task<IActionResult> Refresh(TokenModel model)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(model.Token);
            var username = principal.Identity.Name; //this is mapped to the Name claim by default

            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return NotFound("The user requested doesn't exits");
            if (user.RefreshToken != model.RefreshToken) return BadRequest("Invalid refresh token");
            if (!_tokenService.ValidateRefreshToken(model.RefreshToken)) { return BadRequest("Invalid refresh token"); }
            var newJwtToken = _tokenService.GenerateAccessToken(principal.Claims);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            await _userManager.UpdateAsync(user);

            return new ObjectResult(new TokenModel
            {
                Id = user.Id,
                Token = newJwtToken,
                Expires = DateTime.Now.AddMinutes(_appSettings.AccessTokenExpireTime),
                RefreshToken = newRefreshToken
            });
        }

        [HttpPost, Authorize]
        [Route("revoke")]
        public async Task<IActionResult> Revoke()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null) return NotFound("The user requested doesn't exits");

            user.RefreshToken = null;
            user.Loggedin = false;

            await _userManager.UpdateAsync(user);

            return NoContent();
        }
    }
}