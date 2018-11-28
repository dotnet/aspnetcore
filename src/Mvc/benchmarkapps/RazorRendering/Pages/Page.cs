using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Pages
{
    public class Page : PageModel
    {
        public ILogger Logger { get; }

        public string PageIcon { get; protected set; }
        public string PageTitle { get; protected set; }
        public string PageUrl { get; protected set; }

        public Page(ILogger logger)
        {
            Logger = logger;
        }


        public void AddErrorMessage(string message)
        {
            Response.Headers.Add("X-Error-Message", UrlEncoder.Default.Encode(message));
        }
    }
}