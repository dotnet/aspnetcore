// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Routing.Patterns;

internal sealed class DefaultRoutePatternTransformer : RoutePatternTransformer
{
    private readonly ParameterPolicyFactory _policyFactory;

    public DefaultRoutePatternTransformer(ParameterPolicyFactory policyFactory)
    {
        ArgumentNullException.ThrowIfNull(policyFactory);

        _policyFactory = policyFactory;
    }

    [RequiresUnreferencedCode("This API may perform reflection on supplied parameter which may be trimmed if not referenced directly." +
        "Consider using a different overload to avoid this issue.")]
    public override RoutePattern SubstituteRequiredValues(RoutePattern original, object requiredValues)
    {
        ArgumentNullException.ThrowIfNull(original);

        return SubstituteRequiredValues(original, new RouteValueDictionary(requiredValues));
    }

    public override RoutePattern SubstituteRequiredValues(RoutePattern original, RouteValueDictionary requiredValues)
    {
        ArgumentNullException.ThrowIfNull(original);

        // Process each required value in sequence. Bail if we find any rejection criteria. The goal
        // of rejection is to avoid creating RoutePattern instances that can't *ever* match.
        //
        // If we succeed, then we need to create a new RoutePattern with the provided required values.
        //
        // Substitution can merge with existing RequiredValues already on the RoutePattern as long
        // as all of the success criteria are still met at the end.
        foreach (var kvp in requiredValues)
        {
            // There are three possible cases here:
            // 1. Required value is null-ish
            // 2. Required value is *any*
            // 3. Required value corresponds to a parameter
            // 4. Required value corresponds to a matching default value
            //
            // If none of these are true then we can reject this substitution.
            RoutePatternParameterPart parameter;
            if (RouteValueEqualityComparer.Default.Equals(kvp.Value, string.Empty))
            {
                // 1. Required value is null-ish - check to make sure that this route doesn't have a
                // parameter or filter-like default.

                if (original.GetParameter(kvp.Key) != null)
                {
                    // Fail: we can't 'require' that a parameter be null. In theory this would be possible
                    // for an optional parameter, but that's not really in line with the usage of this feature
                    // so we don't handle it.
                    //
                    // Ex: {controller=Home}/{action=Index}/{id?} - with required values: { controller = "" }
                    return null;
                }
                else if (original.Defaults.TryGetValue(kvp.Key, out var defaultValue) &&
                    !RouteValueEqualityComparer.Default.Equals(kvp.Value, defaultValue))
                {
                    // Fail: this route has a non-parameter default that doesn't match.
                    //
                    // Ex: Admin/{controller=Home}/{action=Index}/{id?} defaults: { area = "Admin" } - with required values: { area = "" }
                    return null;
                }

                // Success: (for this parameter at least)
                //
                // Ex: {controller=Home}/{action=Index}/{id?} - with required values: { area = "", ... }
                continue;
            }
            else if (RoutePattern.IsRequiredValueAny(kvp.Value))
            {
                // 2. Required value is *any* - this is allowed for a parameter with a default, but not
                // a non-parameter default.
                if (original.GetParameter(kvp.Key) == null &&
                    original.Defaults.TryGetValue(kvp.Key, out var defaultValue) &&
                    !RouteValueEqualityComparer.Default.Equals(string.Empty, defaultValue))
                {
                    // Fail: this route as a non-parameter default that is stricter than *any*.
                    //
                    // Ex: Admin/{controller=Home}/{action=Index}/{id?} defaults: { area = "Admin" } - with required values: { area = *any* }
                    return null;
                }

                // Success: (for this parameter at least)
                //
                // Ex: {controller=Home}/{action=Index}/{id?} - with required values: { controller = *any*, ... }
                continue;
            }
            else if ((parameter = original.GetParameter(kvp.Key)) != null)
            {
                // 3. Required value corresponds to a parameter - check to make sure that this value matches
                // any IRouteConstraint implementations.
                if (!MatchesConstraints(original, parameter, kvp.Key, requiredValues))
                {
                    // Fail: this route has a constraint that failed.
                    //
                    // Ex: Admin/{controller:regex(Home|Login)}/{action=Index}/{id?} - with required values: { controller = "Store" }
                    return null;
                }

                // Success: (for this parameter at least)
                //
                // Ex: {area}/{controller=Home}/{action=Index}/{id?} - with required values: { area = "", ... }
                continue;
            }
            else if (original.Defaults.TryGetValue(kvp.Key, out var defaultValue) &&
                RouteValueEqualityComparer.Default.Equals(kvp.Value, defaultValue))
            {
                // 4. Required value corresponds to a matching default value - check to make sure that this value matches
                // any IRouteConstraint implementations. It's unlikely that this would happen in practice but it doesn't
                // hurt for us to check.
                if (!MatchesConstraints(original, parameter: null, kvp.Key, requiredValues))
                {
                    // Fail: this route has a constraint that failed.
                    //
                    // Ex:
                    //  Admin/Home/{action=Index}/{id?}
                    //  defaults: { area = "Admin" }
                    //  constraints: { area = "Blog" }
                    //  with required values: { area = "Admin" }
                    return null;
                }

                // Success: (for this parameter at least)
                //
                // Ex: Admin/{controller=Home}/{action=Index}/{id?} defaults: { area = "Admin" }- with required values: { area = "Admin", ... }
                continue;
            }
            else
            {
                // Fail: this is a required value for a key that doesn't appear in the templates, or the route
                // pattern has a different default value for a non-parameter.
                //
                // Ex: Admin/{controller=Home}/{action=Index}/{id?} defaults: { area = "Admin" }- with required values: { area = "Blog", ... }
                // OR (less likely)
                // Ex: Admin/{controller=Home}/{action=Index}/{id?} with required values: { page = "/Index", ... }
                return null;
            }
        }

        List<RoutePatternParameterPart> updatedParameters = null;
        List<RoutePatternPathSegment> updatedSegments = null;
        RouteValueDictionary updatedDefaults = null;

        // So if we get here, we're ready to update the route pattern. We need to update two things:
        // 1. Remove any default values that conflict with the required values.
        // 2. Merge any existing required values
        foreach (var kvp in requiredValues)
        {
            var parameter = original.GetParameter(kvp.Key);

            // We only need to handle the case where the required value maps to a parameter. That's the only
            // case where we allow a default and a required value to disagree, and we already validated the
            // other cases.
            //
            // If the required value is *any* then don't remove the default.
            if (parameter != null &&
                !RoutePattern.IsRequiredValueAny(kvp.Value) &&
                original.Defaults.TryGetValue(kvp.Key, out var defaultValue) &&
                !RouteValueEqualityComparer.Default.Equals(kvp.Value, defaultValue))
            {
                if (updatedDefaults == null && updatedSegments == null && updatedParameters == null)
                {
                    updatedDefaults = new RouteValueDictionary(original.Defaults);
                    updatedSegments = new List<RoutePatternPathSegment>(original.PathSegments);
                    updatedParameters = new List<RoutePatternParameterPart>(original.Parameters);
                }

                updatedDefaults.Remove(kvp.Key);
                RemoveParameterDefault(updatedSegments, updatedParameters, parameter);
            }
        }

        foreach (var kvp in original.RequiredValues)
        {
            requiredValues.TryAdd(kvp.Key, kvp.Value);
        }

        return new RoutePattern(
            original.RawText,
            updatedDefaults ?? original.Defaults,
            original.ParameterPolicies,
            requiredValues,
            updatedParameters ?? original.Parameters,
            updatedSegments ?? original.PathSegments);
    }

    private bool MatchesConstraints(RoutePattern pattern, RoutePatternParameterPart parameter, string key, RouteValueDictionary requiredValues)
    {
        if (pattern.ParameterPolicies.TryGetValue(key, out var policies))
        {
            for (var i = 0; i < policies.Count; i++)
            {
                var policy = _policyFactory.Create(parameter, policies[i]);
                if (policy is IRouteConstraint constraint)
                {
                    if (!constraint.Match(httpContext: null, NullRouter.Instance, key, requiredValues, RouteDirection.IncomingRequest))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static void RemoveParameterDefault(List<RoutePatternPathSegment> segments, List<RoutePatternParameterPart> parameters, RoutePatternParameterPart parameter)
    {
        // We know that a parameter can only appear once, so we only need to rewrite one segment and one parameter.
        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            for (var j = 0; j < segment.Parts.Count; j++)
            {
                if (object.ReferenceEquals(parameter, segment.Parts[j]))
                {
                    // Found it!
                    var updatedParameter = RoutePatternFactory.ParameterPart(parameter.Name, @default: null, parameter.ParameterKind, parameter.ParameterPolicies);

                    var updatedParts = new List<RoutePatternPart>(segment.Parts);
                    updatedParts[j] = updatedParameter;
                    segments[i] = RoutePatternFactory.Segment(updatedParts);

                    for (var k = 0; k < parameters.Count; k++)
                    {
                        if (ReferenceEquals(parameter, parameters[k]))
                        {
                            parameters[k] = updatedParameter;
                            break;
                        }
                    }

                    return;
                }
            }
        }
    }
}
