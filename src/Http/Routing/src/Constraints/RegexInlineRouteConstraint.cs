// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
#if !COMPONENTS
using Microsoft.AspNetCore.Routing.Matching;
#endif

namespace Microsoft.AspNetCore.Routing.Constraints;

#if !COMPONENTS
/// <summary>
/// Represents a regex constraint which can be used as an inlineConstraint.
/// </summary>
public class RegexInlineRouteConstraint : RegexRouteConstraint, ICachableParameterPolicy
#else
internal class RegexInlineRouteConstraint : RegexRouteConstraint
#endif
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegexInlineRouteConstraint" /> class.
    /// </summary>
    /// <param name="regexPattern">The regular expression pattern to match.</param>
    public RegexInlineRouteConstraint([StringSyntax(StringSyntaxAttribute.Regex, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)] string regexPattern)
        : base(regexPattern)
    {
    }
}
