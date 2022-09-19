// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;

namespace Microsoft.AspNetCore.Mvc.Testing;

/// <summary>
/// The default options to use to when creating
/// <see cref="HttpClient"/> instances by calling
/// <see cref="WebApplicationFactory{TEntryPoint}.CreateClient(WebApplicationFactoryClientOptions)"/>.
/// </summary>
public class WebApplicationFactoryClientOptions
{
    /// <summary>
    /// Initializes a new instance of <see cref="WebApplicationFactoryClientOptions"/>.
    /// </summary>
    public WebApplicationFactoryClientOptions()
    {
    }

    // Copy constructor
    internal WebApplicationFactoryClientOptions(WebApplicationFactoryClientOptions clientOptions)
    {
        BaseAddress = clientOptions.BaseAddress;
        AllowAutoRedirect = clientOptions.AllowAutoRedirect;
        MaxAutomaticRedirections = clientOptions.MaxAutomaticRedirections;
        HandleCookies = clientOptions.HandleCookies;
    }

    /// <summary>
    /// Gets or sets the base address of <see cref="HttpClient"/> instances created by calling
    /// <see cref="WebApplicationFactory{TEntryPoint}.CreateClient(WebApplicationFactoryClientOptions)"/>.
    /// The default is <c>http://localhost</c>.
    /// </summary>
    public Uri BaseAddress { get; set; } = new Uri("http://localhost");

    /// <summary>
    /// Gets or sets whether or not <see cref="HttpClient"/> instances created by calling
    /// <see cref="WebApplicationFactory{TEntryPoint}.CreateClient(WebApplicationFactoryClientOptions)"/>
    /// should automatically follow redirect responses.
    /// The default is <c>true</c>.
    /// </summary>
    public bool AllowAutoRedirect { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of redirect responses that <see cref="HttpClient"/> instances
    /// created by calling <see cref="WebApplicationFactory{TEntryPoint}.CreateClient(WebApplicationFactoryClientOptions)"/>
    /// should follow.
    /// The default is <c>7</c>.
    /// </summary>
    public int MaxAutomaticRedirections { get; set; } = RedirectHandler.DefaultMaxRedirects;

    /// <summary>
    /// Gets or sets whether <see cref="HttpClient"/> instances created by calling
    /// <see cref="WebApplicationFactory{TEntryPoint}.CreateClient(WebApplicationFactoryClientOptions)"/>
    /// should handle cookies.
    /// The default is <c>true</c>.
    /// </summary>
    public bool HandleCookies { get; set; } = true;

    internal DelegatingHandler[] CreateHandlers()
    {
        return CreateHandlersCore().ToArray();

        IEnumerable<DelegatingHandler> CreateHandlersCore()
        {
            if (AllowAutoRedirect)
            {
                yield return new RedirectHandler(MaxAutomaticRedirections);
            }
            if (HandleCookies)
            {
                yield return new CookieContainerHandler();
            }
        }
    }
}
