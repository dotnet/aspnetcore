// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.ShortCircuit;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Short circuit the endpoint(s).
/// The execution of the endpoint will happen in UseRouting middleware.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ShortCircuitAttribute : Attribute, IShortCircuitMetadata
{
    /// <summary>
    /// Constructs an instance of <see cref="ShortCircuitAttribute"/>.
    /// </summary>
    public ShortCircuitAttribute() { }

    /// <inheritdoc />
    public int? StatusCode { get; }
}
