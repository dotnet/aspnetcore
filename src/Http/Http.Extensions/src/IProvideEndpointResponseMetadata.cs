// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Indicates that a type provides a static method that returns <see cref="Endpoint"/> metadata when declared as the
/// returned type of a <see cref="Endpoint"/> route handler delegate. The method must be of the form:
/// <code>public static <see cref="IEnumerable{Object}"/> GetMetadata(<see cref="Endpoint"/> endpoint, <see cref="IServiceProvider"/> services)</code>
/// </summary>
public interface IProvideEndpointResponseMetadata
{
    /// <summary>
    /// Supplies objects to apply as metadata to the related <see cref="Endpoint"/>.
    /// </summary>
    /// <param name="endpoint">The <see cref="Endpoint"/> the returned metadata will be applied to.</param>
    /// <param name="services">The application's <see cref="IServiceProvider"/>.</param>
    /// <returns>The objects to apply as <see cref="Endpoint"/> metadata.</returns>
    public static abstract IEnumerable<object> GetMetadata(Endpoint endpoint, IServiceProvider services);
}
