// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
#if !COMPONENTS
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;
#else
using Microsoft.AspNetCore.Components.Routing;
#endif

namespace Microsoft.AspNetCore.Routing.Constraints;

#if !COMPONENTS
/// <summary>
/// Constrains a route parameter to represent only non-file-name values. Does not validate that
/// the route value contains valid file system characters, or that the value represents
/// an actual file on disk.
/// </summary>
/// <remarks>
/// <para>
/// This constraint can be used to disambiguate requests for dynamic content versus
/// static files served from the application.
/// </para>
/// <para>
/// This constraint determines whether a route value represents a file name by examining
/// the last URL Path segment of the value (delimited by <c>/</c>). The last segment
/// must contain the dot (<c>.</c>) character followed by one or more non-(<c>.</c>) characters.
/// </para>
/// <para>
/// If the route value does not contain a <c>/</c> then the entire value will be interpreted
/// as a the last segment.
/// </para>
/// <para>
/// The <see cref="NonFileNameRouteConstraint"/> does not attempt to validate that the value contains
/// a legal file name for the current operating system.
/// </para>
/// <para>
/// <list type="bullet">
///     <listheader>
///         <term>Examples of route values that will be matched as non-file-names</term>
///         <description>description</description>
///     </listheader>
///     <item>
///         <term><c>/a/b/c</c></term>
///         <description>Final segment does not contain a <c>.</c>.</description>
///     </item>
///     <item>
///         <term><c>/a/b.d/c</c></term>
///         <description>Final segment does not contain a <c>.</c>.</description>
///     </item>
///     <item>
///         <term><c>/a/b.d/c/</c></term>
///         <description>Final segment is empty.</description>
///     </item>
///     <item>
///         <term><c></c></term>
///         <description>Value is empty</description>
///     </item>
/// </list>
/// <list type="bullet">
///     <listheader>
///         <term>Examples of route values that will be rejected as file names</term>
///         <description>description</description>
///     </listheader>
///     <item>
///         <term><c>/a/b/c.txt</c></term>
///         <description>Final segment contains a <c>.</c> followed by other characters.</description>
///     </item>
///     <item>
///         <term><c>/hello.world.txt</c></term>
///         <description>Final segment contains a <c>.</c> followed by other characters.</description>
///     </item>
///     <item>
///         <term><c>hello.world.txt</c></term>
///         <description>Final segment contains a <c>.</c> followed by other characters.</description>
///     </item>
///     <item>
///         <term><c>.gitignore</c></term>
///         <description>Final segment contains a <c>.</c> followed by other characters.</description>
///     </item>
/// </list>
/// </para>
/// </remarks>
public class NonFileNameRouteConstraint : IRouteConstraint, IParameterLiteralNodeMatchingPolicy, ICachableParameterPolicy
#else
internal class NonFileNameRouteConstraint : IRouteConstraint
#endif
{
    /// <inheritdoc />
    public bool Match(
#if !COMPONENTS
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
#else
        string routeKey,
        RouteValueDictionary values)
#endif
    {
        ArgumentNullException.ThrowIfNull(routeKey);
        ArgumentNullException.ThrowIfNull(values);

        if (values.TryGetValue(routeKey, out var obj) && obj != null)
        {
            var value = Convert.ToString(obj, CultureInfo.InvariantCulture);
            return !FileNameRouteConstraint.IsFileName(value);
        }

        // No value or null value.
        //
        // We want to return true here because the core use-case of the constraint is to *exclude*
        // things that look like file names. There's nothing here that looks like a file name, so
        // let it through.
        return true;
    }

#if !COMPONENTS
    bool IParameterLiteralNodeMatchingPolicy.MatchesLiteral(string parameterName, string literal)
    {
        return !FileNameRouteConstraint.IsFileName(literal);
    }
#endif
}
