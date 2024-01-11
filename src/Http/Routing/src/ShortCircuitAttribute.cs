// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Short circuit the endpoint(s).
/// The execution of the endpoint will happen in UseRouting middleware.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ShortCircuitAttribute : Attribute
{
    /// <summary>
    /// Constructs an instance of <see cref="ShortCircuitAttribute"/>.
    /// </summary>
    public ShortCircuitAttribute() { }

    /// <summary>
    /// Constructs an instance of <see cref="ShortCircuitAttribute"/>.
    /// </summary>
    /// <param name="statusCode">The status code to set in the response.</param>
    internal ShortCircuitAttribute(int? statusCode)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// The status code of the response.
    /// </summary>
    public int? StatusCode { get; }
}
