// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ActionConstraints;

/// <summary>
/// Provider for <see cref="IActionConstraint"/>.
/// </summary>
public interface IActionConstraintProvider
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
    /// Called to execute the provider.
    /// <see cref="Order"/> for details on the order of execution of <see cref="OnProvidersExecuting(ActionConstraintProviderContext)"/>.
    /// </summary>
    /// <param name="context">The <see cref="ActionConstraintProviderContext"/>.</param>
    void OnProvidersExecuting(ActionConstraintProviderContext context);

    /// <summary>
    /// Called to execute the provider, after the <see cref="OnProvidersExecuting"/> methods of all providers,
    /// have been called.
    /// <see cref="Order"/> for details on the order of execution of <see cref="OnProvidersExecuted(ActionConstraintProviderContext)"/>.
    /// </summary>
    /// <param name="context">The <see cref="ActionConstraintProviderContext"/>.</param>
    void OnProvidersExecuted(ActionConstraintProviderContext context);
}
