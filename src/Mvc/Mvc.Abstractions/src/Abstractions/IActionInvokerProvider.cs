// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Abstractions;

/// <summary>
/// Defines an interface for components that can create an <see cref="IActionInvoker"/> for the
/// current request.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IActionInvokerProvider"/> instances form a pipeline that results in the creation of an
/// <see cref="IActionInvoker"/>. The <see cref="IActionInvokerProvider"/> instances are ordered by
/// an ascending sort of the <see cref="Order"/>.
/// </para>
/// <para>
/// To create an <see cref="IActionInvoker"/>, each provider has its <see cref="OnProvidersExecuting"/> method
/// called in sequence and given the same instance of <see cref="ActionInvokerProviderContext"/>. Then each
/// provider has its <see cref="OnProvidersExecuted"/> method called in the reverse order. The result is
/// the value of <see cref="ActionInvokerProviderContext.Result"/>.
/// </para>
/// <para>
/// As providers are called in a predefined sequence, each provider has a chance to observe and decorate the
/// result of the providers that have already run.
/// </para>
/// </remarks>
public interface IActionInvokerProvider
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
    /// </summary>
    /// <param name="context">The <see cref="ActionInvokerProviderContext"/>.</param>
    void OnProvidersExecuting(ActionInvokerProviderContext context);

    /// <summary>
    /// Called to execute the provider, after the <see cref="OnProvidersExecuting"/> methods of all providers,
    /// have been called.
    /// </summary>
    /// <param name="context">The <see cref="ActionInvokerProviderContext"/>.</param>
    void OnProvidersExecuted(ActionInvokerProviderContext context);
}
