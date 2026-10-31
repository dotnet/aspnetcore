// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Patterns;

/// <summary>
/// Defines the kinds of <see cref="RoutePatternPart"/> instances.
/// </summary>
#if !COMPONENTS
public enum RoutePatternPartKind
#else
internal enum RoutePatternPartKind
#endif
{
    /// <summary>
    /// The <see cref="RoutePatternPartKind"/> of a <see cref="RoutePatternLiteralPart"/>.
    /// </summary>
    Literal,

    /// <summary>
    /// The <see cref="RoutePatternPartKind"/> of a <see cref="RoutePatternParameterPart"/>.
    /// </summary>
    Parameter,

    /// <summary>
    /// The <see cref="RoutePatternPartKind"/> of a <see cref="RoutePatternSeparatorPart"/>.
    /// </summary>
    Separator,
}
