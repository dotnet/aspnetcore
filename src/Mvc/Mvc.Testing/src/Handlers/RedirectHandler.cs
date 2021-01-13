// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Testing.Handlers
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> that follows redirect responses.
    /// </summary>
    public class RedirectHandler : DelegatingHandler
    {
        internal const int DefaultMaxRedirects = 7;

        /// <summary>
        /// Creates a new instance of <see cref="RedirectHandler"/>.
        /// </summary>
        public RedirectHandler()
            : this(maxRedirects: DefaultMaxRedirects)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="RedirectHandler"/>.
        /// </summary>
        /// <param name="maxRedirects">The maximum number of redirect responses to follow. It must be
        /// equal or greater than 0.</param>
        public RedirectHandler(int maxRedirects)
        {
            if (maxRedirects <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRedirects));
            }

            MaxRedirects = maxRedirects;
        }

        /// <summary>
        /// Gets the maximum number of redirects this handler will follow.
        /// </summary>
        public int MaxRedirects { get; }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var remainingRedirects = MaxRedirects;
            var redirectRequest = new HttpRequestMessage();
            var originalRequestContent = HasBody(request) ? await DuplicateRequestContent(request) : null;
            CopyRequestHeaders(request.Headers, redirectRequest.Headers);
            var response = await base.SendAsync(request, cancellationToken);
            while (IsRedirect(response) && remainingRedirects >= 0)
            {
                remainingRedirects--;
                UpdateRedirectRequest(response, redirectRequest, originalRequestContent);
                originalRequestContent = HasBody(redirectRequest) ? await DuplicateRequestContent(redirectRequest) : null;
                response = await base.SendAsync(redirectRequest, cancellationToken);
            }

            return response;
        }

        private static bool HasBody(HttpRequestMessage request) =>
            request.Method == HttpMethod.Post || request.Method == HttpMethod.Put;

        private static async Task<HttpContent> DuplicateRequestContent(HttpRequestMessage request)
        {
            if (request.Content == null)
            {
                return null;
            }
            var originalRequestContent = request.Content;
            var (originalBody, copy) = await CopyBody(request);

            var contentCopy = new StreamContent(copy);
            request.Content = new StreamContent(originalBody);

            CopyContentHeaders(originalRequestContent, request.Content, contentCopy);

            return contentCopy;
        }

        private static void CopyContentHeaders(
            HttpContent originalRequestContent,
            HttpContent newRequestContent,
            HttpContent contentCopy)
        {
            foreach (var header in originalRequestContent.Headers)
            {
                contentCopy.Headers.TryAddWithoutValidation(header.Key, header.Value);
                newRequestContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        private static void CopyRequestHeaders(
            HttpRequestHeaders originalRequestHeaders,
            HttpRequestHeaders newRequestHeaders)
        {
            foreach (var header in originalRequestHeaders)
            {
                newRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        private static async Task<(Stream originalBody, Stream copy)> CopyBody(HttpRequestMessage request)
        {
            var originalBody = await request.Content.ReadAsStreamAsync();
            var bodyCopy = new MemoryStream();
            await originalBody.CopyToAsync(bodyCopy);
            bodyCopy.Seek(0, SeekOrigin.Begin);
            if (originalBody.CanSeek)
            {
                originalBody.Seek(0, SeekOrigin.Begin);
            }
            else
            {
                originalBody = new MemoryStream();
                await bodyCopy.CopyToAsync(originalBody);
                originalBody.Seek(0, SeekOrigin.Begin);
                bodyCopy.Seek(0, SeekOrigin.Begin);
            }

            return (originalBody, bodyCopy);
        }

        private static void UpdateRedirectRequest(
            HttpResponseMessage response,
            HttpRequestMessage redirect,
            HttpContent originalContent)
        {
            var location = response.Headers.Location;
            if (!location.IsAbsoluteUri)
            {
                location = new Uri(
                    new Uri(response.RequestMessage.RequestUri.GetLeftPart(UriPartial.Authority)),
                    location);
            }

            redirect.RequestUri = location;
            if (!ShouldKeepVerb(response))
            {
                redirect.Method = HttpMethod.Get;
            }
            else
            {
                redirect.Method = response.RequestMessage.Method;
                redirect.Content = originalContent;
            }

            foreach (var property in response.RequestMessage.Properties)
            {
                redirect.Properties.Add(property.Key, property.Value);
            }
        }

        private static bool ShouldKeepVerb(HttpResponseMessage response) =>
            response.StatusCode == HttpStatusCode.RedirectKeepVerb ||
                (int)response.StatusCode == 308;

        private bool IsRedirect(HttpResponseMessage response) =>
            response.StatusCode == HttpStatusCode.MovedPermanently ||
                response.StatusCode == HttpStatusCode.Redirect ||
                response.StatusCode == HttpStatusCode.RedirectKeepVerb ||
                (int)response.StatusCode == 308;
    }
}