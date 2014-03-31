using System.Security.Principal;
using Microsoft.AspNet.Abstractions;
using System.Security.Claims;

namespace Microsoft.AspNet.Mvc
{
    // Provides configuration information about the anti-forgery system.
    internal interface ITokenGenerator
    {
        // Generates a new random cookie token.
        AntiForgeryToken GenerateCookieToken();

        // Given a cookie token, generates a corresponding form token.
        // The incoming cookie token must be valid.
        AntiForgeryToken GenerateFormToken(HttpContext httpContext, ClaimsIdentity identity, AntiForgeryToken cookieToken);
    }
}