// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
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
