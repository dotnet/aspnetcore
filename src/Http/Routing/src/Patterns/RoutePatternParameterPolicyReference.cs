// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Routing.Patterns;

/// <summary>
/// The parsed representation of a policy in a <see cref="RoutePattern"/> parameter. Instances
/// of <see cref="RoutePatternParameterPolicyReference"/> are immutable.
/// </summary>
[DebuggerDisplay("{DebuggerToString()}")]
#if !COMPONENTS
public sealed class RoutePatternParameterPolicyReference
#else
internal sealed class RoutePatternParameterPolicyReference
#endif
{
    internal RoutePatternParameterPolicyReference(string content)
    {
        Content = content;
    }

    internal RoutePatternParameterPolicyReference(IParameterPolicy parameterPolicy)
    {
        ParameterPolicy = parameterPolicy;
    }

    /// <summary>
    /// Gets the constraint text.
    /// </summary>
    public string? Content { get; }

    /// <summary>
    /// Gets a pre-existing <see cref="IParameterPolicy"/> that was used to construct this reference.
    /// </summary>
    public IParameterPolicy? ParameterPolicy { get; }

    private string? DebuggerToString()
    {
        return Content;
    }
}
