// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Options for controlling the behavior of <see cref="EndpointRouteBuilderExtensions.MapGet(IEndpointRouteBuilder, string, Delegate)"/>
/// and similar methods.
/// </summary>
public sealed class RouteHandlerOptions
{
    /// <summary>
    /// Controls whether endpoints should throw a <see cref="BadHttpRequestException"/> in addition to
    /// writing a <see cref="LogLevel.Debug"/> log when handling invalid requests.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="HostEnvironmentEnvExtensions.IsDevelopment(IHostEnvironment)"/>.
    /// </remarks>
    public bool ThrowOnBadRequest { get; set; }
}
