// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Testing.Handlers;

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
        ArgumentOutOfRangeException.ThrowIfNegative(maxRedirects);

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
        while (IsRedirect(response) && remainingRedirects > 0)
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

    private static async Task<HttpContent?> DuplicateRequestContent(HttpRequestMessage request)
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
        HttpRequestHeaders redirectRequestHeaders)
    {
        foreach (var header in originalRequestHeaders)
        {
            // Avoid copying the Authorization header to match the behavior
            // in the HTTP client when processing redirects
            // https://github.com/dotnet/runtime/blob/69b5d67d9418d672609aa6e2c418a3d4ae00ad18/src/libraries/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/SocketsHttpHandler.cs#L509-L517
            if (!header.Key.Equals(HeaderNames.Authorization, StringComparison.OrdinalIgnoreCase))
            {
                redirectRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
    }

    private static async Task<(Stream originalBody, Stream copy)> CopyBody(HttpRequestMessage request)
    {
        var originalBody = await request.Content!.ReadAsStreamAsync();
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
        HttpContent? originalContent)
    {
        Debug.Assert(response.RequestMessage is not null);

        var location = response.Headers.Location;
        if (location != null)
        {
            if (!location.IsAbsoluteUri && response.RequestMessage.RequestUri is Uri requestUri)
            {
                location = new Uri(requestUri, location);
            }

            redirect.RequestUri = location;
        }

        if (!ShouldKeepVerb(response))
        {
            redirect.Method = HttpMethod.Get;
        }
        else
        {
            redirect.Method = response.RequestMessage.Method;
            redirect.Content = originalContent;
        }

        foreach (var property in response.RequestMessage.Options)
        {
            var key = new HttpRequestOptionsKey<object?>(property.Key);
            redirect.Options.Set(key, property.Value);
        }
    }

    private static bool ShouldKeepVerb(HttpResponseMessage response) =>
        response.StatusCode == HttpStatusCode.RedirectKeepVerb ||
            (int)response.StatusCode == 308;

    private static bool IsRedirect(HttpResponseMessage response) =>
        response.StatusCode == HttpStatusCode.MovedPermanently ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.RedirectMethod ||
            response.StatusCode == HttpStatusCode.RedirectKeepVerb ||
            (int)response.StatusCode == 308;
}
