// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Default implementation for <see cref="IHttpRequestLifetimeFeature"/>.
/// </summary>
public class HttpRequestLifetimeFeature : IHttpRequestLifetimeFeature
{
    /// <inheritdoc />
    public CancellationToken RequestAborted { get; set; }

    /// <inheritdoc />
    public void Abort()
    {
    }
}
