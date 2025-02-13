// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;

namespace Microsoft.AspNetCore.TestHost;

/// <summary>
/// A contract for a test server implementation.
/// </summary>
public interface ITestServer : IServer
{
    /// <summary>
    /// Gets the web host associated with the test server.
    /// </summary>
    IWebHost Host { get; }

    /// <summary>
    /// Creates a new <see cref="HttpMessageHandler"/> for processing HTTP requests against the test server.
    /// </summary>
    /// <returns>A new <see cref="HttpMessageHandler"/> instance.</returns>
    HttpMessageHandler CreateHandler();

    /// <summary>
    /// Creates a new <see cref="HttpClient"/> for processing HTTP requests against the test server.
    /// </summary>
    /// <returns></returns>
    HttpClient CreateClient();
}
