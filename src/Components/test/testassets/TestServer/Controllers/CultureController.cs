using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

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

            var htmlEncoder = HtmlEncoder.Default;
            var html = $"<h1>Culture has been changed to {htmlEncoder.Encode(culture)}</h1>" +
                $"<a class='return-from-culture-setter' href='{htmlEncoder.Encode(redirectUri)}'>Return to previous page</a>";
            return Content(html, "text/html");
        }
    }
}
