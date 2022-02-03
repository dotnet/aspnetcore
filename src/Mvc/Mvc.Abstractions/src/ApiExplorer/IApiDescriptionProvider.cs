// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

/// <summary>
/// Defines a contract for specifying <see cref="ApiDescription"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// On the first query for <see cref="ActionDescriptor"/>, MVC invokes all registered instances of <see cref="IApiDescriptionProvider"/>
/// in the ascending sort order of <see cref="Order"/>.
/// </para>
/// <para>
/// Each provider has its <see cref="OnProvidersExecuting"/> method
/// called in sequence and given the same instance of <see cref="ApiDescriptionProviderContext"/>. Then each
/// provider has its <see cref="OnProvidersExecuted"/> method called in the reverse order. Each instance has
/// an opportunity to add or modify <see cref="ApiDescriptionProviderContext.Results"/>.
/// </para>
/// <para>
/// As providers are called in a predefined sequence, each provider has a chance to observe and decorate the
/// result of the providers that have already run.
/// </para>
/// </remarks>
public interface IApiDescriptionProvider
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
    /// Creates or modifies <see cref="ApiDescription"/>s.
    /// </summary>
    /// <param name="context">The <see cref="ApiDescriptionProviderContext"/>.</param>
    void OnProvidersExecuting(ApiDescriptionProviderContext context);

    /// <summary>
    /// Called after <see cref="IApiDescriptionProvider"/> implementations with higher <see cref="Order"/> values have been called.
    /// </summary>
    /// <param name="context">The <see cref="ApiDescriptionProviderContext"/>.</param>
    void OnProvidersExecuted(ApiDescriptionProviderContext context);
}
