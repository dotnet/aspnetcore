// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.AspNetCore.Components.WebAssembly.Http;

/// <summary>
/// Extension methods for configuring an instance of <see cref="HttpRequestMessage"/> with browser-specific options.
/// </summary>
public static class WebAssemblyHttpRequestMessageExtensions
{
    private static readonly HttpRequestOptionsKey<IDictionary<string, object>> FetchRequestOptionsKey = new HttpRequestOptionsKey<IDictionary<string, object>>("WebAssemblyFetchOptions");
    private static readonly HttpRequestOptionsKey<bool> WebAssemblyEnableStreamingRequestKey = new HttpRequestOptionsKey<bool>("WebAssemblyEnableStreamingRequest");
    private static readonly HttpRequestOptionsKey<bool> WebAssemblyEnableStreamingResponseKey = new HttpRequestOptionsKey<bool>("WebAssemblyEnableStreamingResponse");

    /// <summary>
    /// Configures a value for the 'credentials' option for the HTTP request.
    /// </summary>
    /// <param name="requestMessage">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="requestCredentials">The <see cref="BrowserRequestCredentials"/> option.</param>
    /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
    /// <remarks>
    /// See <see href="https://developer.mozilla.org/en-US/docs/Web/API/Request/credentials"/>.
    /// </remarks>
    public static HttpRequestMessage SetBrowserRequestCredentials(this HttpRequestMessage requestMessage, BrowserRequestCredentials requestCredentials)
    {
        ArgumentNullException.ThrowIfNull(requestMessage);

        var stringOption = requestCredentials switch
        {
            BrowserRequestCredentials.Omit => "omit",
            BrowserRequestCredentials.SameOrigin => "same-origin",
            BrowserRequestCredentials.Include => "include",
            _ => throw new InvalidOperationException($"Unsupported enum value {requestCredentials}.")
        };

        return SetBrowserRequestOption(requestMessage, "credentials", stringOption);
    }

    /// <summary>
    /// Configures a value for the 'cache' option for the HTTP request.
    /// </summary>
    /// <param name="requestMessage">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="requestCache">The <see cref="BrowserRequestCache"/> option.</param>
    /// <returns>The <see cref="HttpRequestMessage"/>.</returns>\
    /// <remarks>
    /// See <see href="https://developer.mozilla.org/en-US/docs/Web/API/Request/cache"/>.
    /// </remarks>
    public static HttpRequestMessage SetBrowserRequestCache(this HttpRequestMessage requestMessage, BrowserRequestCache requestCache)
    {
        ArgumentNullException.ThrowIfNull(requestMessage);

        var stringOption = requestCache switch
        {
            BrowserRequestCache.Default => "default",
            BrowserRequestCache.NoStore => "no-store",
            BrowserRequestCache.Reload => "reload",
            BrowserRequestCache.NoCache => "no-cache",
            BrowserRequestCache.ForceCache => "force-cache",
            BrowserRequestCache.OnlyIfCached => "only-if-cached",
            _ => throw new InvalidOperationException($"Unsupported enum value {requestCache}.")
        };

        return SetBrowserRequestOption(requestMessage, "cache", stringOption);
    }

    /// <summary>
    /// Configures a value for the 'mode' option for the HTTP request.
    /// </summary>
    /// <param name="requestMessage">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="requestMode">The <see cref="BrowserRequestMode"/>.</param>
    /// <returns>The <see cref="HttpRequestMessage"/>.</returns>\
    /// <remarks>
    /// See <see href="https://developer.mozilla.org/en-US/docs/Web/API/Request/mode"/>.
    /// </remarks>
    public static HttpRequestMessage SetBrowserRequestMode(this HttpRequestMessage requestMessage, BrowserRequestMode requestMode)
    {
        ArgumentNullException.ThrowIfNull(requestMessage);

        var stringOption = requestMode switch
        {
            BrowserRequestMode.SameOrigin => "same-origin",
            BrowserRequestMode.NoCors => "no-cors",
            BrowserRequestMode.Cors => "cors",
            BrowserRequestMode.Navigate => "navigate",
            _ => throw new InvalidOperationException($"Unsupported enum value {requestMode}.")
        };

        return SetBrowserRequestOption(requestMessage, "mode", stringOption);
    }

    /// <summary>
    /// Configures a value for the 'integrity' option for the HTTP request.
    /// </summary>
    /// <param name="requestMessage">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="integrity">The subresource integrity descriptor.</param>
    /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
    /// <remarks>
    /// See <see href="https://developer.mozilla.org/en-US/docs/Web/API/Request/integrity"/>.
    /// </remarks>
    public static HttpRequestMessage SetBrowserRequestIntegrity(this HttpRequestMessage requestMessage, string integrity)
        => SetBrowserRequestOption(requestMessage, "integrity", integrity);

    /// <summary>
    /// Configures a value for the HTTP request.
    /// </summary>
    /// <param name="requestMessage">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="name">The name of the option, which should correspond to a key defined on <see href="https://fetch.spec.whatwg.org/#requestinit"/>.</param>
    /// <param name="value">The value, which must be JSON-serializable.</param>
    /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
    /// <remarks>
    /// See <see href="https://developer.mozilla.org/en-US/docs/Web/API/WindowOrWorkerGlobalScope/fetch"/>.
    /// </remarks>
    public static HttpRequestMessage SetBrowserRequestOption(this HttpRequestMessage requestMessage, string name, object value)
    {
        ArgumentNullException.ThrowIfNull(requestMessage);

        IDictionary<string, object> fetchOptions;
        if (requestMessage.Options.TryGetValue(FetchRequestOptionsKey, out var entry))
        {
            fetchOptions = entry;
        }
        else
        {
            fetchOptions = new Dictionary<string, object>(StringComparer.Ordinal);
            requestMessage.Options.Set(FetchRequestOptionsKey, fetchOptions);
        }

        fetchOptions[name] = value;

        return requestMessage;
    }

    /// <summary>
    /// Configures streaming request for the HTTP request.
    /// </summary>
    /// <param name="requestMessage">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="streamingEnabled"><see langword="true"/> if streaming is enabled; otherwise <see langword="false"/>.</param>
    /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
    /// <remarks>
    /// This API is only effective when the browser HTTP Fetch supports request streaming.
    /// Requires HTTP/2 or higher server support.
    /// See <see href="https://developer.mozilla.org/en-US/docs/Web/API/Request"/>.
    /// </remarks>
    public static HttpRequestMessage SetBrowserRequestStreamingEnabled(this HttpRequestMessage requestMessage, bool streamingEnabled)
    {
        ArgumentNullException.ThrowIfNull(requestMessage);

        requestMessage.Options.Set(WebAssemblyEnableStreamingRequestKey, streamingEnabled);

        return requestMessage;
    }

    /// <summary>
    /// Configures streaming response for the HTTP request.
    /// </summary>
    /// <param name="requestMessage">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="streamingEnabled"><see langword="true"/> if streaming is enabled; otherwise <see langword="false"/>.</param>
    /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
    /// <remarks>
    /// This API is only effective when the browser HTTP Fetch supports response streaming.
    /// See <see href="https://developer.mozilla.org/en-US/docs/Web/API/Response"/>.
    /// </remarks>
    public static HttpRequestMessage SetBrowserResponseStreamingEnabled(this HttpRequestMessage requestMessage, bool streamingEnabled)
    {
        ArgumentNullException.ThrowIfNull(requestMessage);

        requestMessage.Options.Set(WebAssemblyEnableStreamingResponseKey, streamingEnabled);

        return requestMessage;
    }
}
