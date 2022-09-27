// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.TestHost;

/// <summary>
/// Options for the test server.
/// </summary>
public class TestServerOptions
{
    /// <summary>
    /// Gets or sets a value that controls whether synchronous IO is allowed for the <see cref="HttpContext.Request"/> and <see cref="HttpContext.Response"/>. The default value is <see langword="false" />.
    /// </summary>
    public bool AllowSynchronousIO { get; set; }

    /// <summary>
    /// Gets or sets a value that controls if <see cref="ExecutionContext"/> and <see cref="AsyncLocal{T}"/> values are preserved from the client to the server. The default value is <see langword="false" />.
    /// </summary>
    public bool PreserveExecutionContext { get; set; }

    /// <summary>
    /// Gets or sets the base address associated with the HttpClient returned by the test server. Defaults to http://localhost/.
    /// </summary>
    public Uri BaseAddress { get; set; } = new Uri("http://localhost/");
}
