// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// The result of creating a <see cref="RequestDelegate" /> from a <see cref="Delegate" />
/// </summary>
public sealed class RequestDelegateResult
{
    /// <summary>
    /// Creates a new instance of <see cref="RequestDelegateResult"/>.
    /// </summary>
    public RequestDelegateResult(RequestDelegate requestDelegate, IReadOnlyList<object> metadata)
    {
        RequestDelegate = requestDelegate;
        EndpointMetadata = metadata;
    }

    /// <summary>
    /// Gets the <see cref="RequestDelegate" />
    /// </summary>
    public RequestDelegate RequestDelegate { get; }

    /// <summary>
    /// Gets endpoint metadata inferred from creating the <see cref="RequestDelegate" />. If a non-null
    /// RequestDelegateFactoryOptions.EndpointMetadata list was passed in, this will be the same instance.
    /// </summary>
    public IReadOnlyList<object> EndpointMetadata { get; }
}
