// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Defines the contract that a class must implement to transform route values while building
/// a URI.
/// </summary>
public interface IOutboundParameterTransformer : IParameterPolicy
{
    /// <summary>
    /// Transforms the specified route value to a string for inclusion in a URI.
    /// </summary>
    /// <param name="value">The route value to transform.</param>
    /// <returns>The transformed value.</returns>
    string? TransformOutbound(object? value);
}
