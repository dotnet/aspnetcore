// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public class HtmlPage<TApplicationContext>
    {
        public HtmlPage(HttpClient client, IHtmlDocument document, TApplicationContext context)
        {
            Client = client;
            Document = document;
            Context = context;
        }

        public HttpClient Client { get; }
        public IHtmlDocument Document { get; }
        public TApplicationContext Context { get; }
    }
}
