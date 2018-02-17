using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Pages.Account
{
    public class ExternalLogin : DefaultUIPage
    {
        private readonly IHtmlFormElement _emailForm;

        public ExternalLogin(
            HttpClient client,
            IHtmlDocument externalLogin,
            DefaultUIContext context)
            : base(client, externalLogin, context)
        {
            _emailForm = HtmlAssert.HasForm(Document);
        }

        public async Task<Index> SendEmailAsync(string email)
        {
            var response = await Client.SendAsync(_emailForm, new Dictionary<string, string>
            {
                ["Input_Email"] = email
            });
            var goToIndex = ResponseAssert.IsRedirect(response);
            var indexResponse = await Client.GetAsync(goToIndex);
            var index = await ResponseAssert.IsHtmlDocumentAsync(indexResponse);

            return new Index(Client, index, Context.WithAuthenticatedUser());
        }
    }
}
