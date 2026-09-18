// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;

namespace Microsoft.AspNetCore.Identity.FunctionalTests;

public static class WebApplicationFactoryExtensions
{
    public static HttpClient CreateClient<TStartup>(this WebApplicationFactory<TStartup> factory, CookieContainer container)
        where TStartup : class
    {
        var client = factory.CreateDefaultClient(new CookieContainerHandler(container));
        client.BaseAddress = factory.ClientOptions.BaseAddress;
        return client;
    }
}
