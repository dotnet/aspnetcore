// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !COMPONENTS
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
#else
using Microsoft.AspNetCore.Components.Routing;
#endif

namespace Microsoft.AspNetCore.Routing.Constraints;

/// <summary>
/// A route constraint that negates one or more inner constraints. The constraint matches
/// when none of the inner constraints match the route value.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="NotRouteConstraint"/> implements logical negation for route constraints.
/// It takes a semicolon-separated list of constraint names and returns <c>true</c> only
/// when none of the specified constraints match the route value.
/// </para>
/// <para>
/// <strong>Supported Features:</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Basic type constraints: <c>int</c>, <c>bool</c>, <c>guid</c>, <c>datetime</c>, <c>decimal</c>, <c>double</c>, <c>float</c>, <c>long</c></description>
/// </item>
/// <item>
/// <description>String constraints: <c>alpha</c>, <c>length(n)</c>, <c>minlength(n)</c>, <c>maxlength(n)</c></description>
/// </item>
/// <item>
/// <description>Numeric constraints: <c>min(n)</c>, <c>max(n)</c>, <c>range(min,max)</c></description>
/// </item>
/// <item>
/// <description>File constraints: <c>file</c>, <c>nonfile</c></description>
/// </item>
/// <item>
/// <description>Special constraints: <c>required</c></description>
/// </item>
/// <item>
/// <description>Multiple constraints with semicolon separation (logical AND of negations)</description>
/// </item>
/// <item>
/// <description>Nested negation patterns (e.g., <c>not(not(int))</c>) - fully supported as recursive constraint evaluation</description>
/// </item>
/// </list>
/// <para>
/// <strong>Examples:</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <term><c>not(int)</c></term>
/// <description>Matches any value that is NOT an integer (e.g., "abc", "12.5", "true")</description>
/// </item>
/// <item>
/// <term><c>not(int;bool)</c></term>
/// <description>Matches values that are neither integers nor booleans (e.g., "abc", "12.5")</description>
/// </item>
/// <item>
/// <term><c>not(not(int))</c></term>
/// <description>Double negation - matches integers (equivalent to just using <c>int</c> constraint)</description>
/// </item>
/// <item>
/// <term><c>not(min(18))</c></term>
/// <description>Matches integer values less than 18 or non-integer values</description>
/// </item>
/// <item>
/// <term><c>not(alpha)</c></term>
/// <description>Matches non-alphabetic values (e.g., "123", "test123")</description>
/// </item>
/// <item>
/// <term><c>not(file)</c></term>
/// <description>Matches values that don't contain file extensions</description>
/// </item>
/// </list>
/// <para>
/// <strong>Important Notes:</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Unknown constraint names are ignored and always treated as non-matching, resulting in negation returning <c>true</c></description>
/// </item>
/// <item>
/// <description>Nested negation patterns are fully supported and work recursively (e.g., <c>not(not(int))</c> = double negation)</description>
/// </item>
/// <item>
/// <description>Multiple constraints are combined with logical AND - ALL inner constraints must fail for the negation to succeed</description>
/// </item>
/// <item>
/// <description>Works with both route matching and literal parameter matching scenarios</description>
/// </item>
/// </list>
/// </remarks>
#if !COMPONENTS
public class NotRouteConstraint : IRouteConstraint, IParameterLiteralNodeMatchingPolicy
#else
internal class NotRouteConstraint : IRouteConstraint
#endif
{
    /// <summary>
    /// Gets the array of inner constraint names to be negated.
    /// </summary>
    private string[] _inner { get; }

    /// <summary>
    /// Cached constraint map to avoid repeated reflection-based lookups.
    /// </summary>
    private static IDictionary<string, Type>? _cachedConstraintMap;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotRouteConstraint"/> class
    /// with the specified inner constraints.
    /// </summary>
    /// <param name="constraints">
    /// A semicolon-separated string containing the names of constraints to negate.
    /// Can be a single constraint name (e.g., "int") or multiple constraints (e.g., "int;bool;guid").
    /// Parameterized constraints are supported (e.g., "min(18);length(5)").
    /// Unknown constraint names are treated as non-matching constraints.
    /// </param>
    /// <remarks>
    /// <para>The constraints string is split by semicolons to create individual constraint checks.</para>
    /// <para>Examples of valid constraint strings:</para>
    /// <list type="bullet">
    /// <item><description><c>"int"</c> - Single type constraint</description></item>
    /// <item><description><c>"int;bool"</c> - Multiple type constraints</description></item>
    /// <item><description><c>"min(18)"</c> - Parameterized constraint</description></item>
    /// <item><description><c>"length(5);alpha"</c> - Mixed constraint types</description></item>
    /// <item><description><c>""</c> - Empty string (always returns true)</description></item>
    /// </list>
    /// </remarks>
    public NotRouteConstraint(string constraints)
    {
        _inner = constraints.Split(";");
    }

    private static IDictionary<string, Type> GetConstraintMap()
    {
        // Use cached map or fall back to default constraint map
        return _cachedConstraintMap ??= GetDefaultConstraintMap();
    }

    private static Dictionary<string, Type> GetDefaultConstraintMap()
    {
        // FIXME: I'm not sure if this is a good thing to do because
        // it requires weak spreading between the ConstraintMap and
        // RouteOptions. It doesn't seem appropriate to create two
        // identical variables for this...

        var defaults = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            // Type-specific constraints
            ["int"] = typeof(IntRouteConstraint),
            ["bool"] = typeof(BoolRouteConstraint),
            ["datetime"] = typeof(DateTimeRouteConstraint),
            ["decimal"] = typeof(DecimalRouteConstraint),
            ["double"] = typeof(DoubleRouteConstraint),
            ["float"] = typeof(FloatRouteConstraint),
            ["guid"] = typeof(GuidRouteConstraint),
            ["long"] = typeof(LongRouteConstraint),

            // Length constraints
            ["minlength"] = typeof(MinLengthRouteConstraint),
            ["maxlength"] = typeof(MaxLengthRouteConstraint),
            ["length"] = typeof(LengthRouteConstraint),

            // Min/Max value constraints
            ["min"] = typeof(MinRouteConstraint),
            ["max"] = typeof(MaxRouteConstraint),
            ["range"] = typeof(RangeRouteConstraint),

            // Alpha constraint
            ["alpha"] = typeof(AlphaRouteConstraint),

#if !COMPONENTS
            ["required"] = typeof(RequiredRouteConstraint),
#endif

            // File constraints
            ["file"] = typeof(FileNameRouteConstraint),
            ["nonfile"] = typeof(NonFileNameRouteConstraint),

            // Not constraint
            ["not"] = typeof(NotRouteConstraint)
        };

        return defaults;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// This method implements the core negation logic by:
    /// </para>
    /// <list type="number">
    /// <item>Resolving each inner constraint name to its corresponding <see cref="IRouteConstraint"/> implementation</item>
    /// <item>Testing each resolved constraint against the route value</item>
    /// <item>Returning <c>false</c> immediately if any constraint matches (short-circuit evaluation)</item>
    /// <item>Returning <c>true</c> only if no constraints match</item>
    /// </list>
    /// <para>
    /// The method attempts to use the constraint map from <see cref="RouteOptions"/> if available via 
    /// the HTTP context's service provider, falling back to the default constraint map if needed.
    /// </para>
    /// <para>
    /// Unknown constraint names are ignored (treated as non-matching), which means they don't affect
    /// the negation result.
    /// </para>
    /// </remarks>
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

        // Try to get constraint map from HttpContext first, fallback to default map
        IDictionary<string, Type> constraintMap;
        IServiceProvider? serviceProvider = null;

#if !COMPONENTS
        if (httpContext?.RequestServices != null)
        {
            try
            {
                var routeOptions = httpContext.RequestServices.GetService<IOptions<RouteOptions>>();
                if (routeOptions != null)
                {
                    constraintMap = routeOptions.Value.TrimmerSafeConstraintMap;
                    serviceProvider = httpContext.RequestServices;
                }
                else
                {
                    constraintMap = GetConstraintMap();
                }
            }
            catch
            {
                constraintMap = GetConstraintMap();
            }
        }
        else
        {
            constraintMap = GetConstraintMap();
        }
#else
        constraintMap = GetConstraintMap();
#endif

        foreach (var constraintText in _inner)
        {
            var resolvedConstraint = ParameterPolicyActivator.ResolveParameterPolicy<IRouteConstraint>(
                constraintMap,
                serviceProvider,
                constraintText,
                out _);

            if (resolvedConstraint != null)
            {
                // If any inner constraint matches, return false (negation logic)
#if !COMPONENTS
                if (resolvedConstraint.Match(httpContext, route, routeKey, values, routeDirection))
#else
                if (resolvedConstraint.Match(routeKey, values))
#endif
                {
                    return false;
                }
            }
        }

        // If no inner constraints matched, return true (all constraints were negated)
        return true;
    }

#if !COMPONENTS
    bool IParameterLiteralNodeMatchingPolicy.MatchesLiteral(string parameterName, string literal)
    {
        var constraintMap = GetConstraintMap();

        foreach (var constraintText in _inner)
        {
            var resolvedConstraint = ParameterPolicyActivator.ResolveParameterPolicy<IRouteConstraint>(
                constraintMap,
                null,
                constraintText,
                out _);

            if (resolvedConstraint is IParameterLiteralNodeMatchingPolicy literalPolicy)
            {
                // If any inner constraint matches the literal, return false (negation logic)
                if (literalPolicy.MatchesLiteral(parameterName, literal))
                {
                    return false;
                }
            }
        }

        // If no inner constraints matched the literal, return true
        return true;
    }
#endif
}
