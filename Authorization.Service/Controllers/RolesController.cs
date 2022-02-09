using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Authorization.Service.Controllers
{
    [Route("api/v{v:apiVersion}/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class RolesController : ControllerBase
    {
        private RoleManager<IdentityRole> _roleManager;
        private readonly AppSettings _appSettings;
        public RolesController(RoleManager<IdentityRole> roleManager, IOptions<AppSettings> appsettings)
        {
            _roleManager = roleManager;
            _appSettings = appsettings.Value;
        }

        // GET api/roles
        [HttpGet]
        public ActionResult<IEnumerable<string>> GetAll()
        {
            return Ok(_roleManager.Roles);
        }

        /// <summary>
        /// Get role by Id
        /// </summary>
        /// <param name="id">Id of the role</param>
        /// <response code="200"></response>
        /// <response code="404">The role doesn't exits</response>
        [HttpGet]
        [Route("{id:guid}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<string>>> GetAsync(Guid id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) { return NotFound("The role requested doesn't exits"); }
            return Ok(role);
        }

        // GET api/roles/Admin
        [HttpGet]
        [Route("{name}")]
        public async Task<ActionResult<IEnumerable<string>>> GetAsync(string name)
        {
            var role = await _roleManager.FindByNameAsync(name);
            if (role == null) { return NotFound("The role requested doesn't exits"); }
            return Ok(role);
        }

        // POST api/roles
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] string roleName)
        {

            if (await _roleManager.RoleExistsAsync(roleName)) { return BadRequest("The Role already exists"); }
            var result = await _roleManager.CreateAsync(new IdentityRole { Name = roleName });
            if (!result.Succeeded) { return StatusCode(500); }
            return Ok(result.ToString());
        }

        // PUT api/roles/Admin
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] string name)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) { return NotFound("The requested role doesn't exits on the database."); }
            role.Name = name;
            role.NormalizedName = name.Normalize();
            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded) { return StatusCode(500); }
            return Ok(role);

        }

        // DELETE api/roles/Admin
        [HttpDelete("{roleName}")]
        public async Task<IActionResult> Delete(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null) { return NotFound("The requested role doesn't exits on the database."); }
            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded) { return StatusCode(500); }
            return Ok();
        }
    }
}
