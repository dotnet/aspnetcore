using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage
{
    public class ExternalLogins : DefaultUIPage
    {
        public ExternalLogins(HttpClient client, IHtmlDocument externalLoginDocument, DefaultUIContext context)
            : base(client, externalLoginDocument, context)
        {
        }
    }
}