// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Constraints;

/// <summary>
/// Defines a constraint on an optional parameter whose inner constraint also implements
/// <see cref="IOutboundParameterTransformer"/>. This preserves the transformer capability
/// that would otherwise be lost when wrapping in <see cref="OptionalRouteConstraint"/>.
/// </summary>
internal sealed class OptionalOutboundParameterTransformerRouteConstraint : OptionalRouteConstraint, IOutboundParameterTransformer
{
    /// <summary>
    /// Creates a new <see cref="OptionalOutboundParameterTransformerRouteConstraint"/> instance.
    /// </summary>
    /// <param name="innerConstraint">The inner constraint that also implements <see cref="IOutboundParameterTransformer"/>.</param>
    public OptionalOutboundParameterTransformerRouteConstraint(IRouteConstraint innerConstraint)
        : base(innerConstraint)
    {
    }

    /// <inheritdoc />
    public string? TransformOutbound(object? value)
    {
        return ((IOutboundParameterTransformer)InnerConstraint).TransformOutbound(value);
    }
}
