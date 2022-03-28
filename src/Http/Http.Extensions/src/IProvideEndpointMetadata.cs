// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Indicates that a type provides a static method that returns <see cref="Endpoint"/> metadata when declared as a parameter type, <see cref="Attribute"/> type, or
/// the returned type of an <see cref="Endpoint"/> route handler delegate. The method must be of the form:
/// <code>public static <see cref="IEnumerable{Object}"/> GetMetadata(<see cref="MethodInfo"/> methodInfo, <see cref="IServiceProvider"/> services)</code>
/// </summary>
public interface IProvideEndpointMetadata
{
    /// <summary>
    /// Supplies objects to apply as metadata to the related <see cref="Endpoint"/>.
    /// </summary>
    /// <param name="methodInfo">The <see cref="MethodInfo"/> representing the endpoint route handler delegate.</param>
    /// <param name="services">The application's <see cref="IServiceProvider"/>.</param>
    /// <returns>The objects to apply as <see cref="Endpoint"/> metadata.</returns>
    public static abstract IEnumerable<object> GetMetadata(MethodInfo methodInfo, IServiceProvider services);
}
