// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Network;
using AngleSharp.Parser.Html;
using Xunit;

namespace AuthSamples.FunctionalTests;

// Merged HtmlAssert + ResponseAssert from Identity functional tests
public class TestAssert
{
    public static IHtmlFormElement HasForm(IHtmlDocument document)
    {
        var form = Assert.Single(document.QuerySelectorAll("form"));
        return Assert.IsAssignableFrom<IHtmlFormElement>(form);
    }

    public static IHtmlAnchorElement HasLink(string selector, IHtmlDocument document)
    {
        var element = Assert.Single(document.QuerySelectorAll(selector));
        return Assert.IsAssignableFrom<IHtmlAnchorElement>(element);
    }

    internal static IEnumerable<IHtmlElement> HasElements(string selector, IHtmlDocument document)
    {
        var elements = document
            .QuerySelectorAll(selector)
            .OfType<IHtmlElement>()
            .ToArray();

        Assert.NotEmpty(elements);

        return elements;
    }

    public static IHtmlElement HasElement(string selector, IParentNode document)
    {
        var element = Assert.Single(document.QuerySelectorAll(selector));
        return Assert.IsAssignableFrom<IHtmlElement>(element);
    }

    public static IHtmlFormElement HasForm(string selector, IParentNode document)
    {
        var form = Assert.Single(document.QuerySelectorAll(selector));
        return Assert.IsAssignableFrom<IHtmlFormElement>(form);
    }

    internal static IHtmlHtmlElement IsHtmlFragment(string htmlMessage)
    {
        var synteticNode = $"<div>{htmlMessage}</div>";
        var fragment = Assert.Single(new HtmlParser().ParseFragment(htmlMessage, context: null));
        return Assert.IsAssignableFrom<IHtmlHtmlElement>(fragment);
    }

    public static Uri IsRedirect(HttpResponseMessage responseMessage)
    {
        Assert.Equal(HttpStatusCode.Redirect, responseMessage.StatusCode);
        return responseMessage.Headers.Location;
    }

    public static async Task<IHtmlDocument> IsHtmlDocumentAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);
        var content = await response.Content.ReadAsStringAsync();
        var document = await BrowsingContext.New()
            .OpenAsync(ResponseFactory, CancellationToken.None);
        return Assert.IsAssignableFrom<IHtmlDocument>(document);

        void ResponseFactory(VirtualResponse htmlResponse)
        {
            htmlResponse
                .Address(response.RequestMessage.RequestUri)
                .Status(response.StatusCode);

            MapHeaders(response.Headers);
            MapHeaders(response.Content.Headers);

            htmlResponse.Content(content);

            void MapHeaders(HttpHeaders headers)
            {
                foreach (var header in headers)
                {
                    foreach (var value in header.Value)
                    {
                        htmlResponse.Header(header.Key, value);
                    }
                }
            }
        }
    }

    internal static void IsOK(HttpResponseMessage download)
        => Assert.Equal(HttpStatusCode.OK, download.StatusCode);
}
