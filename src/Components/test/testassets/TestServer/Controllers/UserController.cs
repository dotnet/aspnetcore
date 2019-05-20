using System.Linq;
using BasicTestApp.AuthTest;
using Microsoft.AspNetCore.Mvc;

namespace Components.TestServer.Controllers
{
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        // GET api/user
        [HttpGet]
        public ClientSideAuthenticationStateData Get()
        {
            // Servers are not expected to expose everything from the server-side ClaimsPrincipal
            // to the client. It's up to the developer to choose what kind of authentication state
            // data is needed on the client so it can display suitable options in the UI.

            return new ClientSideAuthenticationStateData
            {
                IsAuthenticated = User.Identity.IsAuthenticated,
                UserName = User.Identity.Name,
                ExposedClaims = User.Claims
                    .Where(c => c.Type == "test-claim")
                    .ToDictionary(c => c.Type, c => c.Value)
            };
        }
    }
}
