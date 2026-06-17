// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Routing.Template;

/// <summary>
/// Supports processing and binding parameter values in a route template.
/// </summary>
public class TemplateBinder
{
    private readonly UrlEncoder _urlEncoder;
    private readonly ObjectPool<UriBuildingContext> _pool;

    private readonly (string parameterName, IRouteConstraint constraint)[] _constraints;
    private readonly RouteValueDictionary? _defaults;
    private readonly KeyValuePair<string, object?>[] _filters;
    private readonly (string parameterName, IOutboundParameterTransformer transformer)[] _parameterTransformers;
    private readonly RoutePattern _pattern;
    private readonly string[] _requiredKeys;

    // A pre-allocated template for the 'known' route values that this template binder uses.
    //
    // We always make a copy of this and operate on the copy, so that we don't mutate shared state.
    private readonly KeyValuePair<string, object?>[] _slots;

    /// <summary>
    /// Creates a new instance of <see cref="TemplateBinder"/>.
    /// </summary>
    /// <param name="urlEncoder">The <see cref="UrlEncoder"/>.</param>
    /// <param name="pool">The <see cref="ObjectPool{T}"/>.</param>
    /// <param name="template">The <see cref="RouteTemplate"/> to bind values to.</param>
    /// <param name="defaults">The default values for <paramref name="template"/>.</param>
    internal TemplateBinder(
        UrlEncoder urlEncoder,
        ObjectPool<UriBuildingContext> pool,
        RouteTemplate template,
        RouteValueDictionary defaults)
        : this(urlEncoder, pool, template?.ToRoutePattern()!, defaults, requiredKeys: null, parameterPolicies: null)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="TemplateBinder"/>.
    /// </summary>
    /// <param name="urlEncoder">The <see cref="UrlEncoder"/>.</param>
    /// <param name="pool">The <see cref="ObjectPool{T}"/>.</param>
    /// <param name="pattern">The <see cref="RoutePattern"/> to bind values to.</param>
    /// <param name="defaults">The default values for <paramref name="pattern"/>. Optional.</param>
    /// <param name="requiredKeys">Keys used to determine if the ambient values apply. Optional.</param>
    /// <param name="parameterPolicies">
    /// A list of (<see cref="string"/>, <see cref="IParameterPolicy"/>) pairs to evaluate when producing a URI.
    /// </param>
    internal TemplateBinder(
        UrlEncoder urlEncoder,
        ObjectPool<UriBuildingContext> pool,
        RoutePattern pattern,
        RouteValueDictionary? defaults,
        IEnumerable<string>? requiredKeys,
        IEnumerable<(string parameterName, IParameterPolicy policy)>? parameterPolicies)
    {
        ArgumentNullException.ThrowIfNull(urlEncoder);
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentNullException.ThrowIfNull(pattern);

        _urlEncoder = urlEncoder;
        _pool = pool;
        _pattern = pattern;
        _defaults = defaults;
        _requiredKeys = requiredKeys?.ToArray() ?? Array.Empty<string>();

        // Any default that doesn't have a corresponding parameter is a 'filter' and if a value
        // is provided for that 'filter' it must match the value in defaults.
        var filters = new RouteValueDictionary(_defaults);
        for (var i = 0; i < pattern.Parameters.Count; i++)
        {
            filters.Remove(pattern.Parameters[i].Name);
        }
        _filters = filters.ToArray();

        Initialize(parameterPolicies, out _constraints, out _parameterTransformers);

        _slots = AssignSlots(_pattern, _filters);
    }

    internal TemplateBinder(
        UrlEncoder urlEncoder,
        ObjectPool<UriBuildingContext> pool,
        RoutePattern pattern,
        IEnumerable<(string parameterName, IParameterPolicy policy)> parameterPolicies)
    {
        ArgumentNullException.ThrowIfNull(urlEncoder);
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentNullException.ThrowIfNull(pattern);

        // Parameter policies can be null.

        _urlEncoder = urlEncoder;
        _pool = pool;
        _pattern = pattern;
        _defaults = new RouteValueDictionary(pattern.Defaults);
        _requiredKeys = pattern.RequiredValues.Keys.ToArray();

        // Any default that doesn't have a corresponding parameter is a 'filter' and if a value
        // is provided for that 'filter' it must match the value in defaults.
        var filters = new RouteValueDictionary(_defaults);
        for (var i = 0; i < pattern.Parameters.Count; i++)
        {
            filters.Remove(pattern.Parameters[i].Name);
        }
        _filters = filters.ToArray();

        Initialize(parameterPolicies, out _constraints, out _parameterTransformers);

        _slots = AssignSlots(_pattern, _filters);
    }

    private static void Initialize(
        IEnumerable<(string parameterName, IParameterPolicy policy)>? parameterPolicies,
        out (string parameterName, IRouteConstraint constraint)[] constraints,
        out (string parameterName, IOutboundParameterTransformer transformer)[] parameterTransformers)
    {
        List<(string parameterName, IRouteConstraint constraint)>? constraintList = null;
        List<(string parameterName, IOutboundParameterTransformer transformer)>? parameterTransformerList = null;

        if (parameterPolicies is not null)
        {
            foreach (var p in parameterPolicies)
            {
                if (p.policy is IRouteConstraint routeConstraint)
                {
                    (constraintList ??= new()).Add((p.parameterName, routeConstraint));
                }
                if (p.policy is IOutboundParameterTransformer transformer)
                {
                    (parameterTransformerList ??= new()).Add((p.parameterName, transformer));
                }
            }
        }

        constraints = constraintList?.ToArray() ?? Array.Empty<(string, IRouteConstraint)>();
        parameterTransformers = parameterTransformerList?.ToArray() ?? Array.Empty<(string, IOutboundParameterTransformer)>();
    }

    /// <summary>
    /// Generates the parameter values in the route.
    /// </summary>
    /// <param name="ambientValues">The values associated with the current request.</param>
    /// <param name="values">The route values to process.</param>
    /// <returns>A <see cref="TemplateValuesResult"/> instance. Can be null.</returns>
    public TemplateValuesResult? GetValues(RouteValueDictionary? ambientValues, RouteValueDictionary values)
    {
        // Make a new copy of the slots array, we'll use this as 'scratch' space
        // and then the RVD will take ownership of it.
        var slots = new KeyValuePair<string, object?>[_slots.Length];
        Array.Copy(_slots, 0, slots, 0, slots.Length);

        // Keeping track of the number of 'values' we've processed can be used to avoid doing
        // some expensive 'merge' operations later.
        var valueProcessedCount = 0;

        // Start by copying all of the values out of the 'values' and into the slots. There's no success
        // case where we *don't* use all of the 'values' so there's no reason not to do this up front
        // to avoid visiting the values dictionary again and again.
        for (var i = 0; i < slots.Length; i++)
        {
            var key = slots[i].Key;
            if (values.TryGetValue(key, out var value))
            {
                // We will need to know later if the value in the 'values' was an null value.
                // This affects how we process ambient values. Since the 'slots' are initialized
                // with null values, we use the null-object-pattern to track 'explicit null', which means that
                // null means omitted.
                value = IsRoutePartNonEmpty(value) ? value : SentinullValue.Instance;
                slots[i] = new KeyValuePair<string, object?>(key, value);

                // Track the count of processed values - this allows a fast path later.
                valueProcessedCount++;
            }
        }

        // In Endpoint Routing, patterns can have logical parameters that appear 'to the left' of
        // the route template. This governs whether or not the template can be selected (they act like
        // filters), and whether the remaining ambient values should be used.
        // should be used.
        // For example, in case of MVC it flattens out a route template like below
        //  {controller}/{action}/{id?}
        // to
        //  Products/Index/{id?},
        //  defaults: new { controller = "Products", action = "Index" },
        //  requiredValues: new { controller = "Products", action = "Index" }
        // In the above example, "controller" and "action" are no longer parameters.
        var copyAmbientValues = ambientValues != null;
        if (copyAmbientValues)
        {
            var requiredKeys = _requiredKeys;
            for (var i = 0; i < requiredKeys.Length; i++)
            {
                // For each required key, the values and ambient values need to have the same value.
                var key = requiredKeys[i];
                var hasExplicitValue = values.TryGetValue(key, out var value);

                if (ambientValues == null || !ambientValues.TryGetValue(key, out var ambientValue))
                {
                    ambientValue = null;
                }

                // For now, only check ambient values with required values that don't have a parameter
                // Ambient values for parameters are processed below
                var hasParameter = _pattern.GetParameter(key) != null;
                if (!hasParameter)
                {
                    if (!_pattern.RequiredValues.TryGetValue(key, out var requiredValue))
                    {
                        throw new InvalidOperationException($"Unable to find required value '{key}' on route pattern.");
                    }

                    if (!RoutePartsEqual(ambientValue, _pattern.RequiredValues[key]) &&
                        !RoutePattern.IsRequiredValueAny(_pattern.RequiredValues[key]))
                    {
                        copyAmbientValues = false;
                        break;
                    }

                    if (hasExplicitValue && !RoutePartsEqual(value, ambientValue))
                    {
                        copyAmbientValues = false;
                        break;
                    }
                }
            }
        }

        // We can now process the rest of the parameters (from left to right) and copy the ambient
        // values as long as the conditions are met.
        //
        // Find out which entries in the URI are valid for the URI we want to generate.
        // If the URI had ordered parameters a="1", b="2", c="3" and the new values
        // specified that b="9", then we need to invalidate everything after it. The new
        // values should then be a="1", b="9", c=<no value>.
        //
        // We also handle the case where a parameter is optional but has no value - we shouldn't
        // accept additional parameters that appear *after* that parameter.
        var parameters = _pattern.Parameters;
        var parameterCount = _pattern.Parameters.Count;
        for (var i = 0; i < parameterCount; i++)
        {
            var key = slots[i].Key;
            var value = slots[i].Value;

            // Whether or not the value was explicitly provided is signficant when comparing
            // ambient values. Remember that we're using a special sentinel value so that we
            // can tell the difference between an omitted value and an explicitly specified null.
            var hasExplicitValue = value != null;

            var hasAmbientValue = false;
            var ambientValue = (object?)null;

            var parameter = parameters[i];

            // We are copying **all** ambient values
            if (copyAmbientValues)
            {
                hasAmbientValue = ambientValues != null && ambientValues.TryGetValue(key, out ambientValue);
                if (hasExplicitValue && hasAmbientValue && !RoutePartsEqual(ambientValue, value))
                {
                    // Stop copying current values when we find one that doesn't match
                    copyAmbientValues = false;
                }

                if (!hasExplicitValue &&
                    !hasAmbientValue &&
                    _defaults?.ContainsKey(parameter.Name) != true)
                {
                    // This is an unsatisfied parameter value and there are no defaults. We might still
                    // be able to generate a URL but we should stop 'accepting' ambient values.
                    //
                    // This might be a case like:
                    //  template: a/{b?}/{c?}
                    //  ambient: { c = 17 }
                    //  values: { }
                    //
                    // We can still generate a URL from this ("/a") but we shouldn't accept 'c' because
                    // we can't use it.
                    //
                    // In the example above we should fall into this block for 'b'.
                    copyAmbientValues = false;
                }
            }

            // This might be an ambient value that matches a required value. We want to use these even if we're
            // not bulk-copying ambient values.
            //
            // This comes up in a case like the following:
            //  ambient-values: { page = "/DeleteUser", area = "Admin", }
            //  values: { controller = "Home", action = "Index", }
            //  pattern: {area}/{controller}/{action}/{id?}
            //  required-values: { area = "Admin", controller = "Home", action = "Index", page = (string)null, }
            //
            // OR in plain English... when linking from a page in an area to an action in the same area, it should
            // be possible to use the area as an ambient value.
            if (!copyAmbientValues && !hasExplicitValue && _pattern.RequiredValues.TryGetValue(key, out var requiredValue))
            {
                hasAmbientValue = ambientValues != null && ambientValues.TryGetValue(key, out ambientValue);
                if (hasAmbientValue &&
                    (RoutePartsEqual(requiredValue, ambientValue) || RoutePattern.IsRequiredValueAny(requiredValue)))
                {
                    // Treat this an an explicit value to *force it*.
                    slots[i] = new KeyValuePair<string, object?>(key, ambientValue);
                    hasExplicitValue = true;
                    value = ambientValue;
                }
            }

            // If the parameter is a match, add it to the list of values we will use for URI generation
            if (hasExplicitValue && !ReferenceEquals(value, SentinullValue.Instance))
            {
                // Already has a value in the list, do nothing
            }
            else if (copyAmbientValues && hasAmbientValue)
            {
                slots[i] = new KeyValuePair<string, object?>(key, ambientValue);
            }
            else if (parameter.IsOptional || parameter.IsCatchAll)
            {
                // Value isn't needed for optional or catchall parameters - wipe out the key, so it
                // will be omitted from the RVD.
                slots[i] = default;
            }
            else if (_defaults != null && _defaults.TryGetValue(parameter.Name, out var defaultValue))
            {
                // Add the default value only if there isn't already a new value for it and
                // only if it actually has a default value.
                slots[i] = new KeyValuePair<string, object?>(key, defaultValue);
            }
            else
            {
                // If we get here, this parameter needs a value, but doesn't have one. This is a
                // failure case.
                return null;
            }
        }

        // Any default values that don't appear as parameters are treated like filters. Any new values
        // provided must match these defaults.
        var filters = _filters;
        for (var i = 0; i < filters.Length; i++)
        {
            var key = filters[i].Key;
            var value = slots[i + parameterCount].Value;

            // We use a sentinel value here so we can track the different between omission and explicit null.
            // 'real null' means that the value was omitted.
            var hasExplicitValue = value != null;
            if (hasExplicitValue)
            {
                // If there is a non-parameterized value in the route and there is a
                // new value for it and it doesn't match, this route won't match.
                if (!RoutePartsEqual(value, filters[i].Value))
                {
                    return null;
                }
            }
            else
            {
                // If no value was provided, then blank out this slot so that it doesn't show up in accepted values.
                slots[i + parameterCount] = default;
            }
        }

        // At this point we've captured all of the 'known' route values, but we have't
        // handled an extra route values that were provided in 'values'. These all
        // need to be included in the accepted values.
        var acceptedValues = RouteValueDictionary.FromArray(slots);

        if (valueProcessedCount < values.Count)
        {
            // There are some values in 'value' that are unaccounted for, merge them into
            // the dictionary.
            foreach (var kvp in values)
            {
                if (!_defaults!.ContainsKey(kvp.Key))
                {
#if RVD_TryAdd
                        acceptedValues.TryAdd(kvp.Key, kvp.Value);
#else
                    if (!acceptedValues.ContainsKey(kvp.Key))
                    {
                        acceptedValues.Add(kvp.Key, kvp.Value);
                    }
#endif
                }
            }
        }

        // Currently this copy is required because BindValues will mutate the accepted values :(
        var combinedValues = new RouteValueDictionary(acceptedValues);

        // Add any ambient values that don't match parameters - they need to be visible to constraints
        // but they will ignored by link generation.
        CopyNonParameterAmbientValues(
            ambientValues: ambientValues,
            acceptedValues: acceptedValues,
            combinedValues: combinedValues);

        return new TemplateValuesResult()
        {
            AcceptedValues = acceptedValues,
            CombinedValues = combinedValues,
        };
    }

    // Step 1.5: Process constraints
    /// <summary>
    /// Processes the constraints **if** they were passed in to the TemplateBinder constructor.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
    /// <param name="combinedValues">A dictionary that contains the parameters for the route.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="constraint">The constraint object.</param>
    /// <returns><see langword="true"/> if constraints were processed succesfully and false otherwise.</returns>
    public bool TryProcessConstraints(HttpContext? httpContext, RouteValueDictionary combinedValues, out string? parameterName, out IRouteConstraint? constraint)
    {
        var constraints = _constraints;
        for (var i = 0; i < constraints.Length; i++)
        {
            (parameterName, constraint) = constraints[i];

            if (!constraint.Match(httpContext, NullRouter.Instance, parameterName, combinedValues, RouteDirection.UrlGeneration))
            {
                return false;
            }
        }

        parameterName = null;
        constraint = null;
        return true;
    }

    // Step 2: If the route is a match generate the appropriate URI
    /// <summary>
    /// Returns a string representation of the URI associated with the route.
    /// </summary>
    /// <param name="acceptedValues">A dictionary that contains the parameters for the route.</param>
    /// <returns>The string representation of the route.</returns>
    public string? BindValues(RouteValueDictionary acceptedValues)
    {
        var context = _pool.Get();

        try
        {
            return TryBindValuesCore(context, acceptedValues) ? context.ToString() : null;
        }
        finally
        {
            _pool.Return(context);
        }
    }

    // Step 2: If the route is a match generate the appropriate URI
    internal bool TryBindValues(
        RouteValueDictionary acceptedValues,
        LinkOptions? options,
        LinkOptions globalOptions,
        out (PathString path, QueryString query) result)
    {
        var context = _pool.Get();

        context.AppendTrailingSlash = options?.AppendTrailingSlash ?? globalOptions.AppendTrailingSlash ?? false;
        context.LowercaseQueryStrings = options?.LowercaseQueryStrings ?? globalOptions.LowercaseQueryStrings ?? false;
        context.LowercaseUrls = options?.LowercaseUrls ?? globalOptions.LowercaseUrls ?? false;

        try
        {
            if (TryBindValuesCore(context, acceptedValues))
            {
                result = (context.ToPathString(), context.ToQueryString());
                return true;
            }

            result = default;
            return false;
        }
        finally
        {
            _pool.Return(context);
        }
    }

    private bool TryBindValuesCore(UriBuildingContext context, RouteValueDictionary acceptedValues)
    {
        // If we have any output parameter transformers, allow them a chance to influence the parameter values
        // before we build the URI.
        var parameterTransformers = _parameterTransformers;
        for (var i = 0; i < parameterTransformers.Length; i++)
        {
            (var parameterName, var transformer) = parameterTransformers[i];
            if (acceptedValues.TryGetValue(parameterName, out var value))
            {
                acceptedValues[parameterName] = transformer.TransformOutbound(value);
            }
        }

        var segments = _pattern.PathSegments;
        // Read interface .Count once rather than per iteration
        var segmentsCount = segments.Count;
        for (var i = 0; i < segmentsCount; i++)
        {
            Debug.Assert(context.BufferState == SegmentState.Beginning);
            Debug.Assert(context.UriState == SegmentState.Beginning);

            var parts = segments[i].Parts;
            // Read interface .Count once rather than per iteration
            var partsCount = parts.Count;
            for (var j = 0; j < partsCount; j++)
            {
                var part = parts[j];
                if (part is RoutePatternLiteralPart literalPart)
                {
                    if (!context.Accept(literalPart.Content))
                    {
                        return false;
                    }
                }
                else if (part is RoutePatternSeparatorPart separatorPart)
                {
                    if (!context.Accept(separatorPart.Content))
                    {
                        return false;
                    }
                }
                else if (part is RoutePatternParameterPart parameterPart)
                {
                    // If it's a parameter, get its value
                    acceptedValues.Remove(parameterPart.Name, out var value);

                    var isSameAsDefault = false;
                    if (_defaults != null &&
                        _defaults.TryGetValue(parameterPart.Name, out var defaultValue) &&
                        RoutePartsEqual(value, defaultValue))
                    {
                        isSameAsDefault = true;
                    }

                    var converted = Convert.ToString(value, CultureInfo.InvariantCulture);
                    if (isSameAsDefault)
                    {
                        // If the accepted value is the same as the default value buffer it since
                        // we won't necessarily add it to the URI we generate.
                        if (!context.Buffer(converted))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // If the value is not accepted, it is null or empty value in the
                        // middle of the segment. We accept this if the parameter is an
                        // optional parameter and it is preceded by an optional seperator.
                        // In this case, we need to remove the optional seperator that we
                        // have added to the URI
                        // Example: template = {id}.{format?}. parameters: id=5
                        // In this case after we have generated "5.", we wont find any value
                        // for format, so we remove '.' and generate 5.
                        if (!context.Accept(converted, parameterPart.EncodeSlashes))
                        {
                            if (j != 0 && parameterPart.IsOptional && parts[j - 1] is RoutePatternSeparatorPart)
                            {
                                context.Remove();
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            context.EndSegment();
        }

        // Generate the query string from the remaining values
        var wroteFirst = false;
        foreach (var kvp in acceptedValues)
        {
            if (_defaults != null && _defaults.ContainsKey(kvp.Key))
            {
                // This value is a 'filter' we don't need to put it in the query string.
                continue;
            }

            var values = kvp.Value as IEnumerable;
            if (values != null && !(values is string))
            {
                foreach (var value in values)
                {
                    wroteFirst |= AddQueryKeyValueToContext(context, kvp.Key, value, wroteFirst);
                }
            }
            else
            {
                wroteFirst |= AddQueryKeyValueToContext(context, kvp.Key, kvp.Value, wroteFirst);
            }
        }

        return true;
    }

    private bool AddQueryKeyValueToContext(UriBuildingContext context, string key, object? value, bool wroteFirst)
    {
        var converted = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(converted))
        {
            if (context.LowercaseQueryStrings)
            {
                key = key.ToLowerInvariant();
                converted = converted.ToLowerInvariant();
            }

            context.QueryWriter.Write(wroteFirst ? '&' : '?');
            _urlEncoder.Encode(context.QueryWriter, key);
            context.QueryWriter.Write('=');
            _urlEncoder.Encode(context.QueryWriter, converted);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Compares two objects for equality as parts of a case-insensitive path.
    /// </summary>
    /// <param name="a">An object to compare.</param>
    /// <param name="b">An object to compare.</param>
    /// <returns>True if the object are equal, otherwise false.</returns>
    public static bool RoutePartsEqual(object? a, object? b)
    {
        var sa = a as string ?? (ReferenceEquals(SentinullValue.Instance, a) ? string.Empty : null);
        var sb = b as string ?? (ReferenceEquals(SentinullValue.Instance, b) ? string.Empty : null);

        // In case of strings, consider empty and null the same.
        // Since null cannot tell us the type, consider it to be a string if the other value is a string.
        if ((sa == string.Empty && sb == null) || (sb == string.Empty && sa == null))
        {
            return true;
        }
        else if (sa != null && sb != null)
        {
            // For strings do a case-insensitive comparison
            return string.Equals(sa, sb, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            if (a != null && b != null)
            {
                // Explicitly call .Equals() in case it is overridden in the type
                return a.Equals(b);
            }
            else
            {
                // At least one of them is null. Return true if they both are
                return a == b;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsRoutePartNonEmpty(object? part)
    {
        if (part == null)
        {
            return false;
        }

        if (ReferenceEquals(SentinullValue.Instance, part))
        {
            return false;
        }

        if (part is string stringPart && stringPart.Length == 0)
        {
            return false;
        }

        return true;
    }

    private void CopyNonParameterAmbientValues(
        RouteValueDictionary? ambientValues,
        RouteValueDictionary acceptedValues,
        RouteValueDictionary combinedValues)
    {
        if (ambientValues == null)
        {
            return;
        }

        foreach (var kvp in ambientValues)
        {
            if (IsRoutePartNonEmpty(kvp.Value))
            {
                var parameter = _pattern.GetParameter(kvp.Key);
                if (parameter == null && !acceptedValues.ContainsKey(kvp.Key))
                {
                    combinedValues.Add(kvp.Key, kvp.Value);
                }
            }
        }
    }

    private static KeyValuePair<string, object?>[] AssignSlots(RoutePattern pattern, KeyValuePair<string, object?>[] filters)
    {
        var slots = new KeyValuePair<string, object?>[pattern.Parameters.Count + filters.Length];

        for (var i = 0; i < pattern.Parameters.Count; i++)
        {
            slots[i] = new KeyValuePair<string, object?>(pattern.Parameters[i].Name, null);
        }

        for (var i = 0; i < filters.Length; i++)
        {
            slots[i + pattern.Parameters.Count] = new KeyValuePair<string, object?>(filters[i].Key, null);
        }

        return slots;
    }

    // This represents an 'explicit null' in the slots array.
    [DebuggerDisplay("explicit null")]
    private sealed class SentinullValue
    {
        public static object Instance = new SentinullValue();

        private SentinullValue()
        {
        }

        public override string ToString() => string.Empty;
    }
}
