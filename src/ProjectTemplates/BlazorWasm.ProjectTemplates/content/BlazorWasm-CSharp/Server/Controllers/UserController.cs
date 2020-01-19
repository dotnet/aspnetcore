using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlazorWasm_CSharp.Shared.Authorization;

namespace BlazorWasm_CSharp.Server.Controllers
{
    [ApiController]
    public class UserController : ControllerBase
    {
        [HttpGet("/User")]
        [Authorize]
        [AllowAnonymous]
        public IActionResult GetCurrentUser() =>
            Ok(User.Identity.IsAuthenticated ? CreateUserInfo(User) : UserInfo.Anonymous);

        private UserInfo CreateUserInfo(ClaimsPrincipal claimsPrincipal)
        {
            if (!claimsPrincipal.Identity.IsAuthenticated)
            {
                return UserInfo.Anonymous;
            }

            var userInfo = new UserInfo();
            userInfo.IsAuthenticated = true;

            if (claimsPrincipal.Identity is ClaimsIdentity claimsIdentity)
            {
                userInfo.NameClaimType = claimsIdentity.NameClaimType;
                userInfo.RoleClaimType = claimsIdentity.RoleClaimType;
            }
            else
            {
                userInfo.NameClaimType = "name";
                userInfo.RoleClaimType = "roles";
            }

            if (claimsPrincipal.Claims.Any())
            {
                var claims = new List<ClaimValue>();
                var nameClaims = claimsPrincipal.FindAll(userInfo.NameClaimType);
                foreach (var claim in nameClaims)
                {
                    claims.Add(new ClaimValue(userInfo.NameClaimType, claim.Value));
                }

                // Uncomment this code if you want to send additional claims to the client.
                //foreach (var claim in claimsPrincipal.Claims.Except(nameClaims))
                //{
                //    claims.Add(new ClaimValue(claim.Type, claim.Value));
                //}

                userInfo.Claims = claims;
            }

            return userInfo;
        }
    }
}
