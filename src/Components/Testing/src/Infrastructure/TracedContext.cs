// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// Wraps an <see cref="IBrowserContext"/> and its associated <see cref="TracingSession"/>
/// into a single <see cref="IAsyncDisposable"/> unit. Provides <see cref="NewPageAsync"/>
/// convenience method and exposes the underlying <see cref="Context"/> property for full API access.
/// </summary>
/// <remarks>
/// <para>Supports deconstruction for concise code:</para>
/// <code>
/// await using var traced = await this.NewTracedContextAsync(server);
/// var (context) = traced;
/// var page = await context.NewPageAsync();
/// </code>
/// <para>
/// C# does not allow implicit conversions to interfaces, so use <see cref="Context"/>
/// when you need the full <see cref="IBrowserContext"/> (e.g., for <c>AddCookiesAsync</c>, <c>RouteAsync</c>).
/// </para>
/// </remarks>
public sealed class TracedContext : IAsyncDisposable
{
    private readonly TracingSession _tracing;

    /// <summary>
    /// The underlying browser context.
    /// </summary>
    public IBrowserContext Context { get; }

    internal TracedContext(IBrowserContext context, TracingSession tracing)
    {
        Context = context;
        _tracing = tracing;
    }

    /// <summary>
    /// Deconstructs the traced context to extract the underlying <see cref="IBrowserContext"/>.
    /// </summary>
    /// <param name="context">The underlying browser context.</param>
    public void Deconstruct(out IBrowserContext context) => context = Context;

    /// <summary>
    /// Creates a new page in the traced browser context.
    /// </summary>
    /// <returns>A new <see cref="IPage"/> in the traced context.</returns>
    public Task<IPage> NewPageAsync() => Context.NewPageAsync();

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => _tracing.DisposeAsync();
}
