using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account
{
    public class ForgotPasswordConfirmation : DefaultUIPage
    {
        public ForgotPasswordConfirmation(HttpClient client, IHtmlDocument document, DefaultUIContext context) : base(client, document, context)
        {
        }
    }
}
