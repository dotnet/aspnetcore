using BlazorWasm_CSharp.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlazorWasm_CSharp.Server.Controllers
{
    [ApiController]
    public class UserController : ControllerBase
    {
        [HttpGet("/User")]
        [Authorize]
        [AllowAnonymous]
        public IActionResult GetCurrentUser() =>
            Ok(User.Identity.IsAuthenticated ? new UserInfo(User) : UserInfo.Anonymous);
    }
}
