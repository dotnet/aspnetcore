using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Pages.Account
{
    public class ConfirmEmail : DefaultUIPage
    {
        public ConfirmEmail(
            HttpClient client,
            IHtmlDocument document,
            DefaultUIContext context) : base(client, document, context)
        {
        }

        public static async Task<ConfirmEmail> Create(IHtmlAnchorElement link, HttpClient client, DefaultUIContext context)
        {
            var response = await client.GetAsync(link.Href);
            var confirmEmail = await ResponseAssert.IsHtmlDocumentAsync(response);

            return new ConfirmEmail(client, confirmEmail, context);
        }
    }
}
