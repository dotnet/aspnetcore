// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Routing.Constraints;

/// <summary>
/// An <see cref="OptionalRouteConstraint"/> that preserves the <see cref="IOutboundParameterTransformer"/>
/// implemented by its inner constraint. Wrapping a constraint for an optional parameter would otherwise hide
/// the transformer from URL generation, so <c>TransformOutbound</c> would never be called for that parameter.
/// </summary>
internal sealed class OptionalOutboundParameterTransformerRouteConstraint : OptionalRouteConstraint, IOutboundParameterTransformer
{
    public OptionalOutboundParameterTransformerRouteConstraint(IRouteConstraint innerConstraint)
        : base(innerConstraint)
    {
        Debug.Assert(
            innerConstraint is IOutboundParameterTransformer,
            $"{nameof(innerConstraint)} must implement {nameof(IOutboundParameterTransformer)}.");
    }

    /// <inheritdoc />
    public string? TransformOutbound(object? value)
    {
        return ((IOutboundParameterTransformer)InnerConstraint).TransformOutbound(value);
    }
}
