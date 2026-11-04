// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A <see cref="FilterItem"/> provider. Implementations should update <see cref="FilterProviderContext.Results"/>
/// to make executable filters available.
/// </summary>
public interface IFilterProvider
{
    /// <summary>
    /// Gets the order value for determining the order of execution of providers. Providers execute in
    /// ascending numeric value of the <see cref="Order"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Providers are executed in an ordering determined by an ascending sort of the <see cref="Order"/> property.
    /// A provider with a lower numeric value of <see cref="Order"/> will have its
    /// <see cref="OnProvidersExecuting"/> called before that of a provider with a higher numeric value of
    /// <see cref="Order"/>. The <see cref="OnProvidersExecuted"/> method is called in the reverse ordering after
    /// all calls to <see cref="OnProvidersExecuting"/>. A provider with a lower numeric value of
    /// <see cref="Order"/> will have its <see cref="OnProvidersExecuted"/> method called after that of a provider
    /// with a higher numeric value of <see cref="Order"/>.
    /// </para>
    /// <para>
    /// If two providers have the same numeric value of <see cref="Order"/>, then their relative execution order
    /// is undefined.
    /// </para>
    /// </remarks>
    int Order { get; }

    /// <summary>
    /// Called in increasing <see cref="Order"/>.
    /// </summary>
    /// <param name="context">The <see cref="FilterProviderContext"/>.</param>
    void OnProvidersExecuting(FilterProviderContext context);

    /// <summary>
    /// Called in decreasing <see cref="Order"/>, after all <see cref="IFilterProvider"/>s have executed once.
    /// </summary>
    /// <param name="context">The <see cref="FilterProviderContext"/>.</param>
    void OnProvidersExecuted(FilterProviderContext context);
}
