// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public static class HttpClientExtensions
{
    public static async Task<IHtmlDocument> GetHtmlDocumentAsync(this HttpClient client, string requestUri)
    {
        var response = await client.GetAsync(requestUri);
        await AssertStatusCodeAsync(response, HttpStatusCode.OK);

        return await GetHtmlDocumentAsync(response);
    }

    public static async Task<IHtmlDocument> GetHtmlDocumentAsync(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var parser = new HtmlParser();
        var document = parser.Parse(content);
        if (document == null)
        {
            throw new InvalidOperationException("Response content could not be parsed as HTML: " + Environment.NewLine + content);
        }

        return document;
    }

    public static async Task<HttpResponseMessage> AssertStatusCodeAsync(this HttpResponseMessage response, HttpStatusCode expectedStatusCode)
    {
        if (response.StatusCode == expectedStatusCode)
        {
            return response;
        }

        string responseContent = string.Join(Environment.NewLine, response.Headers);
        try
        {
            responseContent = await response.Content.ReadAsStringAsync();
        }
        catch
        {
            // No-op
        }

        throw EqualException.ForMismatchedValues(expectedStatusCode, response.StatusCode, $"Expected status code {expectedStatusCode}. Actual {response.StatusCode}. Response Content:" + Environment.NewLine + responseContent);
    }
}
