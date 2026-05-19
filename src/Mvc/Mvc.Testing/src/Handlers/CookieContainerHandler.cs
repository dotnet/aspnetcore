// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Testing.Handlers;

/// <summary>
/// A <see cref="DelegatingHandler"/> that manages cookies associated with one or
/// more pairs of <see cref="HttpRequestMessage"/> and <see cref="HttpResponseMessage"/>.
/// </summary>
public class CookieContainerHandler : DelegatingHandler
{
    /// <summary>
    /// Creates a new instance of <see cref="CookieContainerHandler"/>.
    /// </summary>
    public CookieContainerHandler()
        : this(new CookieContainer())
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="CookieContainerHandler"/>.
    /// </summary>
    /// <param name="cookieContainer">The <see cref="CookieContainer"/> to use for
    /// storing and retrieving cookies.
    /// </param>
    public CookieContainerHandler(CookieContainer cookieContainer)
    {
        Container = cookieContainer;
    }

    /// <summary>
    /// Gets the <see cref="CookieContainer"/> used to store and retrieve cookies.
    /// </summary>
    public CookieContainer Container { get; }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cookieHeader = Container.GetCookieHeader(request.RequestUri!);

        if (!string.IsNullOrEmpty(cookieHeader))
        {
            request.Headers.Add(HeaderNames.Cookie, cookieHeader);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.Headers.TryGetValues(HeaderNames.SetCookie, out var setCookieHeaders))
        {
            foreach (var header in setCookieHeaders)
            {
                Container.SetCookies(response.RequestMessage!.RequestUri!, header);
            }
        }

        return response;
    }
}
