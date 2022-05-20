// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Defines a mechanism for creating an instance of a type from an <see cref="HttpContext"/> when binding parameters for an endpoint
/// route handler delegate.
/// </summary>
/// <typeparam name="TSelf">The type that implements this interface.</typeparam>
public interface IBindableFromHttpContext<TSelf> where TSelf : class, IBindableFromHttpContext<TSelf>
{
    /// <summary>
    /// Creates an instance of <typeparamref name="TSelf"/> from the <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    /// <param name="parameter">The <see cref="ParameterInfo"/> for the parameter of the route handler delegate the returned instance will populate.</param>
    /// <returns>The instance of <typeparamref name="TSelf"/>.</returns>
    static abstract ValueTask<TSelf?> BindAsync(HttpContext context, ParameterInfo parameter);
}
