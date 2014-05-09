using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using System.Security.Claims;

namespace MvcSample.Web
{
    public class FakeUserAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(AuthorizationContext context)
        {
            context.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[] { 
                        new Claim("Permission", "CanViewPage"),
                        new Claim(ClaimTypes.Role, "Administrator"), 
                        new Claim(ClaimTypes.NameIdentifier, "John")}, 
                        "Basic"));
        }
    }
}
