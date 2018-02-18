using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Pages.Account
{
    public class ResetPasswordConfirmation : DefaultUIPage
    {
        public ResetPasswordConfirmation(HttpClient client, IHtmlDocument resetPasswordConfirmation, DefaultUIContext context)
            : base(client, resetPasswordConfirmation, context)
        {
        }
    }
}