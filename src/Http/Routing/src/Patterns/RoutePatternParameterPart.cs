// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace Microsoft.AspNetCore.Routing.Patterns;

/// <summary>
/// Represents a parameter part in a route pattern. Instances of <see cref="RoutePatternParameterPart"/>
/// are immutable.
/// </summary>
[DebuggerDisplay("{DebuggerToString()}")]
#if !COMPONENTS
public sealed class RoutePatternParameterPart : RoutePatternPart
#else
internal sealed class RoutePatternParameterPart : RoutePatternPart
#endif
{
    internal RoutePatternParameterPart(
        string parameterName,
        object? @default,
        RoutePatternParameterKind parameterKind,
        RoutePatternParameterPolicyReference[] parameterPolicies)
        : this(parameterName, @default, parameterKind, parameterPolicies, encodeSlashes: true)
    {
    }

    internal RoutePatternParameterPart(
        string parameterName,
        object? @default,
        RoutePatternParameterKind parameterKind,
        RoutePatternParameterPolicyReference[] parameterPolicies,
        bool encodeSlashes)
        : base(RoutePatternPartKind.Parameter)
    {
        // See #475 - this code should have some asserts, but it can't because of the design of RouteParameterParser.

        Name = parameterName;
        Default = @default;
        ParameterKind = parameterKind;
        ParameterPolicies = parameterPolicies;
        EncodeSlashes = encodeSlashes;
    }

    /// <summary>
    /// Gets the list of parameter policies associated with this parameter.
    /// </summary>
    public IReadOnlyList<RoutePatternParameterPolicyReference> ParameterPolicies { get; }

    /// <summary>
    /// Gets the value indicating if slashes in current parameter's value should be encoded.
    /// </summary>
    public bool EncodeSlashes { get; }

    /// <summary>
    /// Gets the default value of this route parameter. May be null.
    /// </summary>
    public object? Default { get; }

    /// <summary>
    /// Returns <c>true</c> if this part is a catch-all parameter.
    /// Otherwise returns <c>false</c>.
    /// </summary>
    public bool IsCatchAll => ParameterKind == RoutePatternParameterKind.CatchAll;

    /// <summary>
    /// Returns <c>true</c> if this part is an optional parameter.
    /// Otherwise returns <c>false</c>.
    /// </summary>
    public bool IsOptional => ParameterKind == RoutePatternParameterKind.Optional;

    /// <summary>
    /// Gets the <see cref="RoutePatternParameterKind"/> of this parameter.
    /// </summary>
    public RoutePatternParameterKind ParameterKind { get; }

    /// <summary>
    /// Gets the parameter name.
    /// </summary>
    public string Name { get; }

    internal override string DebuggerToString()
    {
        var builder = new StringBuilder();
        builder.Append('{');

        if (IsCatchAll)
        {
            builder.Append('*');
            if (!EncodeSlashes)
            {
                builder.Append('*');
            }
        }

        builder.Append(Name);

        foreach (var constraint in ParameterPolicies)
        {
            builder.Append(':');
            builder.Append(constraint.ParameterPolicy);
        }

        if (Default != null)
        {
            builder.Append('=');
            builder.Append(Default);
        }

        if (IsOptional)
        {
            builder.Append('?');
        }

        builder.Append('}');
        return builder.ToString();
    }
}
