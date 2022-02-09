using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Authorization.Service.Data;
using Authorization.Service.Helpers;
using Authorization.Service.Models;
using Authorization.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Authorization.Service.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{v:apiVersion}/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private UserManager<ApplicationUser> _userManager;
        private readonly AppSettings _appSettings;
        private readonly ITokenServices _tokenServices;

        public UsersController(UserManager<ApplicationUser> userManager, IOptions<AppSettings> appSettings)
        {
            _userManager = userManager;
            _appSettings = appSettings.Value;
            _tokenServices = new TokenService(_appSettings);
        }

        /// <summary>
        /// Authenticate a user to obtain an access token
        /// </summary>
        /// <param name="model"></param>
        /// <response code="200">
        ///		Returns:
        ///			{
        ///				Token: valid access token,
        ///				RefreshToken: valid refresh token used for renew an expired acces token
        ///			}
        ///	</response>
        /// <response code="401">If the username or password are incorrect</response>
        [HttpPost]
        [Route("token")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var claims = new List<Claim>();
                foreach (var claim in await _userManager.GetClaimsAsync(user))
                {
                    claims.Add(claim);
                }
                var token = _tokenServices.GenerateAccessToken(claims);
                var refreshToken = _tokenServices.GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.LastLogin = DateTime.Now;
                user.Loggedin = true;
                await _userManager.UpdateAsync(user);

                return Ok(new TokenModel
                {
                    Id = user.Id,
                    Username = user.UserName,
                    Token = token,
                    Expires = DateTime.Now.AddMinutes(_appSettings.AccessTokenExpireTime),
                    RefreshToken = refreshToken
                });

            }

            return Unauthorized();
        }
        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="model"></param>
        /// <response code="200">
        ///		Returns:
        ///			{
        ///				Token: valid access token,
        ///				RefreshToken: valid refresh token used for renew an expired acces token
        ///			}
        ///	</response>
        /// <response code="400">If the username or email already exits</response>
        /// <response code="500">Internal error during the process</response>
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            #region validation

            if (!ValidationHelper.IsValidEmail(model.Email)) { return BadRequest($"The email '{model.Email}' is not a valid email address format."); }


            if (await _userManager.FindByNameAsync(model.Username) != null) { return BadRequest($"This username '{model.Username}' already exits on the database."); };

            if (await _userManager.FindByEmailAsync(model.Email) != null) { return BadRequest($"This email address '{model.Email}' already exits on the database."); };

            #endregion

            #region Create User
            var newUser = new ApplicationUser
            {
                Name = model.Name,
                LastName = model.Lastname,
                UserName = model.Username,
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                LastLogin = DateTime.Now,
                Avatar = AvatarGenerator.Generate(model.Name, model.Lastname)
            };

            var result = await _userManager.CreateAsync(newUser, model.Password);
            if (result != IdentityResult.Success) { return StatusCode(500, result.Errors.FirstOrDefault().Description); }

            //We asign by default a user role to the user that it is the basic role.
            result = await _userManager.AddToRoleAsync(newUser, "User");
            if (!result.Succeeded) { return StatusCode(500, result.Errors.FirstOrDefault().Description); }

            if (!(await _userManager.GetClaimsAsync(newUser)).Any(c => c.Type == ClaimTypes.Role))
            {
                foreach (var role in await _userManager.GetRolesAsync(newUser))
                {
                    result = await _userManager.AddClaimAsync(newUser, new Claim(ClaimTypes.Role, role));
                    if (!result.Succeeded) { return StatusCode(500, result.Errors.FirstOrDefault().Description); }
                }

            }
            var usersClaims = new[]
            {
                        new Claim(ClaimTypes.Name, newUser.UserName),
                        new Claim(ClaimTypes.NameIdentifier, newUser.Id.ToString())
                    };
            result = await _userManager.AddClaimsAsync(newUser, usersClaims);
            if (!result.Succeeded) { return StatusCode(500, result.Errors.FirstOrDefault().Description); }
            #endregion

            #region Authorize the new user
            var claims = new List<Claim>();

            foreach (var claim in await _userManager.GetClaimsAsync(newUser))
            {
                claims.Add(claim);
            }

            var token = _tokenServices.GenerateAccessToken(claims);
            var refreshToken = _tokenServices.GenerateRefreshToken();

            newUser.RefreshToken = refreshToken;
            await _userManager.UpdateAsync(newUser);
            #endregion

            return Ok(new TokenModel
            {
                Username = newUser.UserName,
                Token = token,
                Expires = DateTime.Now.AddMinutes(_appSettings.AccessTokenExpireTime),
                RefreshToken = refreshToken
            });



        }

        /// <summary>
        /// Add a user to the specified Role
        /// </summary>
        /// <param name="id">Id of the user</param>
        /// <param name="roleName">Name of role</param>
        /// <response code="200"></response>
        /// <response code="400">The user is already in this role</response>
        /// <response code="404">The user doesn't exits</response>
        /// <response code="500">Internal error during the process</response>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("{id}/roles")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AddToRole(string id, [FromBody] string roleName)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) { return NotFound("The user requested doesn't exits on the database."); }
            if (await _userManager.IsInRoleAsync(user, roleName)) { return BadRequest("The user already is in the role."); }
            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded) { return StatusCode(500, result.Errors.FirstOrDefault().Description); }
            result = await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, roleName));
            if (!result.Succeeded) { return StatusCode(500, result.Errors.FirstOrDefault().Description); }
            return Ok();

        }

        /// <summary>
        /// Remove a user from the specified Role
        /// </summary>
        /// <param name="id">Id of the user</param>
        /// <param name="roleName">Name of role</param>
        /// <response code="200"></response>
        /// <response code="400">The user is not in this role</response>
        /// <response code="404">The user doesn't exits</response>
        /// <response code="500">Internal error during the process</response>
        [HttpDelete]
        [Authorize(Roles = "Admin")]
        [Route("{id}/roles")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> RemoveFromRole(string id, [FromBody] string roleName)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) { return NotFound("The user requested doesn't exits on the database."); }
            if (!await _userManager.IsInRoleAsync(user, roleName)) { return BadRequest("The user is not in the role."); }
            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (!result.Succeeded) { return StatusCode(500, result.Errors.FirstOrDefault().Description); }
            result = await _userManager.RemoveClaimAsync(user, new Claim(ClaimTypes.Role, roleName));
            if (!result.Succeeded) { return StatusCode(500, result.Errors.FirstOrDefault().Description); }
            return Ok();

        }
        /// <summary>
        /// List the existing users
        /// </summary>
        /// <response code="200">List of users</response>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public IActionResult GetAll()
        {
            return Ok(new
            {
                users = Mapper.ToUsersResponse(_userManager.Users.ToList())
            });
        }

        /// <summary>
        /// Find user by ID
        /// </summary>
        /// <response code="200">User</response>
        /// <response code="401">Unauthorize</response>
        /// <response code="404">User not found</response>
        [Authorize]
        [HttpGet]
        [Route("{id:guid}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) { return NotFound("The user requested doesn't exits"); }
            return Ok(Mapper.ToUserResponse(user));
        }

        /// <summary>
        /// Find a user by his username
        /// </summary>
        /// <param name="username"></param>
        /// <response code="200">User</response>
        /// <response code="401">Unauthorize</response>
        /// <response code="404">User not found</response>
        [Authorize]
        [HttpGet]
        [Route("{username}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetByUsername(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) { return NotFound("The user requested doesn't exits"); }
            return Ok(Mapper.ToUserResponse(user));
        }

        /// <summary>
        /// Find a user by his email
        /// </summary>
        /// <param name="email"></param>
        /// <response code="200">User</response>
        /// <response code="401">Unauthorize</response>
        /// <response code="404">User not found</response>
        [Authorize]
        [HttpGet]
        [Route("email")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetByEmail([FromBody] string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) { return NotFound($"The user requested with the email: {email} doesn't exits"); }
            return Ok(Mapper.ToUserResponse(user));
        }

        /// <summary>
        /// Update user details
        /// </summary>
        /// <param name="username"></param>
        /// <param name="model"></param>
        /// <response code="200">User updated</response>
        /// <response code="401">Unauthorize</response>
        /// <response code="404">User not found</response>
        /// <response code="403">Operation not allow</response>
        /// <response code="500">Something wrong happen during the update.</response>
        [Authorize]
        [HttpPut]
        [Route("{username}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Update(string username, [FromBody] UpdateUserModel model)
        {
            var user = await _userManager.FindByNameAsync(username);
            var requester = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null) { return NotFound($"The user requested to update doesn't exits"); }
            if (user.NormalizedUserName != requester.NormalizedUserName) { return StatusCode(403); }
            if (!string.IsNullOrWhiteSpace(model.Name)) { user.Name = model.Name; }
            if (!string.IsNullOrWhiteSpace(model.Lastname)) { user.LastName = model.Lastname; }
            if (!string.IsNullOrWhiteSpace(model.Email)) { user.Email = model.Email; }
            if (model.Avatar != null && model.Avatar.Length > 0)
            {
                user.Avatar = model.Avatar;
            }
            else
            {
                user.Avatar = AvatarGenerator.Generate(user.Name, user.LastName);
            }

            var result = await _userManager.UpdateAsync(user);
            if (result != IdentityResult.Success) { return StatusCode(500, result.Errors.FirstOrDefault().Description); }

            return Ok(Mapper.ToUserResponse(user));
        }

        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="username"></param>
        /// <param name="model"></param>
        /// <response code="200">Password updated</response>
        /// <response code="401">Unauthorize</response>
        /// <response code="404">User not found</response>
        /// <response code="403">Operation not allow</response>
        /// <response code="500">Something wrong happen during the update.</response>
        [Authorize]
        [HttpPut]
        [Route("{username}/password")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ChangePassword(string username, [FromBody] ChangePasswordModel model)
        {
            var user = await _userManager.FindByNameAsync(username);
            var requester = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user != null)
            {
                if (user.NormalizedUserName != requester.NormalizedUserName) { return StatusCode(403); }
                var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                if (result != IdentityResult.Success) { return StatusCode(500, result.Errors.FirstOrDefault().Description); }
            }
            else
            {
                return NotFound($"The user requested doesn't exits!");
            }
            return Ok();
        }

    }
}