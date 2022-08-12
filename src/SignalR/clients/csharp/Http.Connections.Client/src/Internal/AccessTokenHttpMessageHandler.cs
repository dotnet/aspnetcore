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
        var isNegotiate = false;
        if (string.IsNullOrEmpty(_accessToken) ||
            // Negotiate redirects likely will have a new access token so let's always grab a (potentially) new access token on negotiate
#if NET5_0_OR_GREATER
            request.Options.TryGetValue(new HttpRequestOptionsKey<bool>("IsNegotiate"), out var value) && value == true
#else
            request.Properties.TryGetValue("IsNegotiate", out var value) && value is true
#endif
            )
        {
            isNegotiate = true;
            _accessToken = await _httpConnection.GetAccessTokenAsync().ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(_accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        var result = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        // retry once with a new token on auth failure
        if (!isNegotiate && result.StatusCode is HttpStatusCode.Unauthorized)
        {
            HttpConnection.Log.RetryAccessToken(_httpConnection._logger, result.StatusCode);
            result.Dispose();
            _accessToken = await _httpConnection.GetAccessTokenAsync().ConfigureAwait(false);

            if (!string.IsNullOrEmpty(_accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }
            // Retrying the request relies on any HttpContent being non-disposable.
            // Currently this is true, the only HttpContent we send is type ReadOnlySequenceContent which is used by SSE and LongPolling for sending an already buffered byte[]
            result = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        return result;
    }
}
