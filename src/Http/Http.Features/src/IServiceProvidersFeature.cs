// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Provides acccess to the request-scoped <see cref="IServiceProvider"/>.
/// </summary>
public interface IServiceProvidersFeature
{
    /// <summary>
    /// Gets or sets the <see cref="IServiceProvider"/> scoped to the current request.
    /// </summary>
    IServiceProvider RequestServices { get; set; }
}
