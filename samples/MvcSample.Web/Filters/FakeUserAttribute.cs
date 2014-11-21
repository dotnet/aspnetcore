using System.Security.Claims;
using Microsoft.AspNet.Mvc;

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
