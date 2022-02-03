// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Used to determine if a handler wants to participate in request processing.
/// </summary>
public interface IAuthenticationRequestHandler : IAuthenticationHandler
{
    /// <summary>
    /// Gets a value that determines if the request should stop being processed.
    /// <para>
    /// This feature is supported by the Authentication middleware
    /// which does not invoke any subsequent <see cref="IAuthenticationHandler"/> or middleware configured in the request pipeline
    /// if the handler returns <see langword="true" />.
    /// </para>
    /// </summary>
    /// <returns><see langword="true" /> if request processing should stop.</returns>
    Task<bool> HandleRequestAsync();
}
