// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
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

            throw new StatusCodeMismatchException
            {
                ExpectedStatusCode = expectedStatusCode,
                ActualStatusCode = response.StatusCode,
                ResponseContent = responseContent,
            };
        }

        private class StatusCodeMismatchException : XunitException
        {
            public HttpStatusCode ExpectedStatusCode { get; set; }

            public HttpStatusCode ActualStatusCode { get; set; }

            public string ResponseContent { get; set; }

            public override string Message
            {
                get
                {
                    return $"Excepted status code {ExpectedStatusCode}. Actual {ActualStatusCode}. Response Content:" + Environment.NewLine + ResponseContent;
                }
            }
        }
    }
}