// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Hosting.Server.Abstractions;

/// <summary>
/// When implemented by a Server allows an <see cref="IHttpApplication{TContext}"/> to pool and reuse
/// its <typeparamref name="TContext"/> between requests.
/// </summary>
/// <typeparam name="TContext">The <see cref="IHttpApplication{TContext}"/> Host context</typeparam>
public interface IHostContextContainer<TContext> where TContext : notnull
{
    /// <summary>
    /// Represents the <typeparamref name="TContext"/>  of the host.
    /// </summary>
    TContext? HostContext { get; set; }
}
