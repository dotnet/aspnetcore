// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests;

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
