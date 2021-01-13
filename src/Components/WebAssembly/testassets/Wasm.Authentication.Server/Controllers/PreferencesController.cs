using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Wasm.Authentication.Server.Data;
using Wasm.Authentication.Server.Models;

namespace Wasm.Authentication.Server.Controllers
{
    [ApiController]
    [Authorize]
    public class PreferencesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PreferencesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("[controller]/[action]")]
        public IActionResult HasCompletedAdditionalInformation()
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier);
            if (!_context.UserPreferences.Where(u => u.ApplicationUserId == id.Value).Any())
            {
                return Ok(false);
            }
            else
            {
                return Ok(true);
            }
        }

        [HttpPost("[controller]/[action]")]
        public async Task<IActionResult> AddPreferences([FromBody] UserPreference preferences)
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier);
            if (!_context.UserPreferences.Where(u => u.ApplicationUserId == id.Value).Any())
            {
                preferences.ApplicationUserId = id.Value;
                _context.UserPreferences.Add(preferences);
                await _context.SaveChangesAsync();
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }
    }
}
