using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public class DefaultUIPage : HtmlPage<DefaultUIContext>
    {
        public DefaultUIPage(HttpClient client, IHtmlDocument document, DefaultUIContext context)
            : base(client, document, context)
        {
        }
    }
}
