// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.TestHost;

/// <summary>
/// Contains extensions for retrieving properties from <see cref="IHost"/>.
/// </summary>
public static class HostBuilderTestServerExtensions
{
    /// <summary>
    /// Retrieves the TestServer from the host services.
    /// </summary>
    /// <param name="host"></param>
    /// <returns></returns>
    public static TestServer GetTestServer(this IHost host)
    {
        return (TestServer)host.Services.GetRequiredService<IServer>();
    }

    /// <summary>
    /// Retrieves the test client from the TestServer in the host services.
    /// </summary>
    /// <param name="host"></param>
    /// <returns></returns>
    public static HttpClient GetTestClient(this IHost host)
    {
        return host.GetTestServer().CreateClient();
    }
}
