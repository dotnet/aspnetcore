// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Patterns;

#if !COMPONENTS
/// <summary>
/// Defines the kinds of <see cref="RoutePatternParameterPart"/> instances.
/// </summary>
public enum RoutePatternParameterKind
#else
internal enum RoutePatternParameterKind
#endif
{
    /// <summary>
    /// The <see cref="RoutePatternParameterKind"/> of a standard parameter
    /// without optional or catch all behavior.
    /// </summary>
    Standard,

    /// <summary>
    /// The <see cref="RoutePatternParameterKind"/> of an optional parameter.
    /// </summary>
    Optional,

    /// <summary>
    /// The <see cref="RoutePatternParameterKind"/> of a catch-all parameter.
    /// </summary>
    CatchAll,
}
