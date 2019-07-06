using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Components.TestServer.Controllers
{
    [Route("[controller]/[action]")]
    public class CultureController : Controller
    {
        public IActionResult SetCulture(string culture, string redirectUri)
        {
            if (culture != null)
            {
                HttpContext.Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName, 
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)));
            }

            return LocalRedirect(redirectUri);
        }
    }
}
