// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
#if !COMPONENTS
using Microsoft.AspNetCore.Routing.Matching;
#endif

namespace Microsoft.AspNetCore.Routing.Constraints;

#if !COMPONENTS
/// <summary>
/// Constrains a route parameter to contain only lowercase or uppercase letters A through Z in the English alphabet.
/// </summary>
public partial class AlphaRouteConstraint : RegexRouteConstraint, ICachableParameterPolicy
#else
internal partial class AlphaRouteConstraint : RegexRouteConstraint
#endif
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AlphaRouteConstraint" /> class.
    /// </summary>
    public AlphaRouteConstraint() : base(GetAlphaRouteRegex())
    {
    }

    [GeneratedRegex(@"^[A-Za-z]*$")]
    private static partial Regex GetAlphaRouteRegex();
}
