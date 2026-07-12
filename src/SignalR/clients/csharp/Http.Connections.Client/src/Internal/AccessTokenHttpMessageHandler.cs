// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal;

internal sealed class AccessTokenHttpMessageHandler : DelegatingHandler
{
    private readonly HttpConnection _httpConnection;
    private string? _accessToken;

    public AccessTokenHttpMessageHandler(HttpMessageHandler inner, HttpConnection httpConnection) : base(inner)
    {
        _httpConnection = httpConnection;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
#if NET5_0_OR_GREATER
        var isNegotiate = request.Options.TryGetValue(new HttpRequestOptionsKey<bool>("IsNegotiate"), out var negotiateValue) && negotiateValue;
        var isRefresh = request.Options.TryGetValue(new HttpRequestOptionsKey<bool>("IsRefresh"), out var refreshValue) && refreshValue;
#else
        var isNegotiate = request.Properties.TryGetValue("IsNegotiate", out var negotiateValue) && negotiateValue is true;
        var isRefresh = request.Properties.TryGetValue("IsRefresh", out var refreshValue) && refreshValue is true;
#endif

        var shouldRetry = true;
        // The token to attach to this request. Defaults to the cached token for normal transport
        // requests (polls/sends), which must keep using whatever the cache currently holds.
        var tokenForRequest = _accessToken;
        if (string.IsNullOrEmpty(_accessToken) || isNegotiate || isRefresh)
        {
            shouldRetry = false;
            // Negotiate redirects likely will have a new access token so let's always grab a (potentially) new access token on negotiate.
            // Authentication refresh exists specifically to obtain a new access token, so always re-fetch on refresh too.
            tokenForRequest = await _httpConnection.GetAccessTokenAsync().ConfigureAwait(false);

            // For negotiate (and the initial fetch) adopt the new token immediately. For refresh, defer
            // updating the cache until we know the server accepted the refresh (below): a rejected
            // refresh (for example an OnAuthenticationRefresh denial returning 403) must not poison the cache and
            // leak the rejected token onto subsequent transport requests (e.g. Long Polling polls/sends).
            if (!isRefresh)
            {
                _accessToken = tokenForRequest;
            }
        }

        SetAccessToken(tokenForRequest, request);

        var result = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (isRefresh)
        {
            // Only adopt the refreshed token once the server has accepted the refresh, so subsequent
            // transport requests use the new token. On rejection, keep the previously cached token.
            if (result.IsSuccessStatusCode)
            {
                _accessToken = tokenForRequest;
            }
        }
        // retry once with a new token on auth failure
        else if (shouldRetry && result.StatusCode is HttpStatusCode.Unauthorized)
        {
            HttpConnection.Log.RetryAccessToken(_httpConnection._logger, result.StatusCode);
            result.Dispose();
            _accessToken = await _httpConnection.GetAccessTokenAsync().ConfigureAwait(false);

            SetAccessToken(_accessToken, request);

            // Retrying the request relies on any HttpContent being non-disposable.
            // Currently this is true, the only HttpContent we send is type ReadOnlySequenceContent which is used by SSE and LongPolling for sending an already buffered byte[]
            result = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        return result;
    }

    private static void SetAccessToken(string? accessToken, HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(accessToken))
        {
            // Don't need to worry about WebSockets and browser because this code path will not be hit in the browser case
            // ClientWebSocketOptions.HttpVersion isn't settable in the browser
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }
}
