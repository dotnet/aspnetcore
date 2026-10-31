// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !COMPONENTS
using System.Diagnostics;
#else
using Microsoft.AspNetCore.Components.Routing;
#endif
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Routing.Constraints;

namespace Microsoft.AspNetCore.Routing;

#if !COMPONENTS
/// <summary>
/// Represents the configurable options on a route.
/// </summary>
public class RouteOptions
#else
internal class RouteOptions
#endif
{
    private IDictionary<string, Type> _constraintTypeMap = GetDefaultConstraintMap();
#if !COMPONENTS
    private ICollection<EndpointDataSource> _endpointDataSources = default!;

    /// <summary>
    /// Gets a collection of <see cref="EndpointDataSource"/> instances configured with routing.
    /// </summary>
    internal ICollection<EndpointDataSource> EndpointDataSources
    {
        get
        {
            Debug.Assert(_endpointDataSources != null, "Endpoint data sources should have been set in DI.");
            return _endpointDataSources;
        }
        set => _endpointDataSources = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether all generated paths URLs are lowercase.
    /// Use <see cref="LowercaseQueryStrings" /> to configure the behavior for query strings.
    /// </summary>
    public bool LowercaseUrls { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a generated query strings are lowercase.
    /// This property will not be used unless <see cref="LowercaseUrls" /> is also <c>true</c>.
    /// </summary>
    public bool LowercaseQueryStrings { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a trailing slash should be appended to the generated URLs.
    /// </summary>
    public bool AppendTrailingSlash { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates if the check for unhandled security endpoint metadata is suppressed.
    /// <para>
    /// Endpoints can be associated with metadata such as authorization, or CORS, that needs to be
    /// handled by a specific middleware to be actionable. If the middleware is not configured, such
    /// metadata will go unhandled.
    /// </para>
    /// <para>
    /// When <see langword="false"/>, prior to the execution of the endpoint, routing will verify that
    /// all known security-specific metadata has been handled.
    /// Setting this property to <see langword="true"/> suppresses this check.
    /// </para>
    /// </summary>
    /// <value>Defaults to <see langword="false"/>.</value>
    /// <remarks>
    /// This check exists as a safeguard against accidental insecure configuration. You may suppress
    /// this check if it does not match your application's requirements.
    /// </remarks>
    public bool SuppressCheckForUnhandledSecurityMetadata { get; set; }
#endif

    /// <summary>
    /// Gets or sets a collection of constraints on the current route.
    /// </summary>
    public IDictionary<string, Type> ConstraintMap
    {
        [RequiresUnreferencedCode($"The linker cannot determine what constraints are being added via the ConstraintMap property. Prefer {nameof(RouteOptions)}.{nameof(SetParameterPolicy)} instead for setting constraints. This warning can be suppressed if this property is being used to read or delete constraints.")]
        get
        {
            return _constraintTypeMap;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _constraintTypeMap = value;
        }
    }

    /// <summary>
    /// <see cref="SetParameterPolicy{T}(string)"/> ensures that types are added to the constraint map in a trimmer safe way.
    /// This API allows reading the map without encountering a trimmer warning within the framework.
    /// </summary>
    internal IDictionary<string, Type> TrimmerSafeConstraintMap => _constraintTypeMap;

    private static IDictionary<string, Type> GetDefaultConstraintMap()
    {
        var defaults = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        // Type-specific constraints
        AddConstraint<IntRouteConstraint>(defaults, "int");
        AddConstraint<BoolRouteConstraint>(defaults, "bool");
        AddConstraint<DateTimeRouteConstraint>(defaults, "datetime");
        AddConstraint<DecimalRouteConstraint>(defaults, "decimal");
        AddConstraint<DoubleRouteConstraint>(defaults, "double");
        AddConstraint<FloatRouteConstraint>(defaults, "float");
        AddConstraint<GuidRouteConstraint>(defaults, "guid");
        AddConstraint<LongRouteConstraint>(defaults, "long");

        // Length constraints
        AddConstraint<MinLengthRouteConstraint>(defaults, "minlength");
        AddConstraint<MaxLengthRouteConstraint>(defaults, "maxlength");
        AddConstraint<LengthRouteConstraint>(defaults, "length");

        // Min/Max value constraints
        AddConstraint<MinRouteConstraint>(defaults, "min");
        AddConstraint<MaxRouteConstraint>(defaults, "max");
        AddConstraint<RangeRouteConstraint>(defaults, "range");

        // The alpha constraint uses a compiled regex which has a minimal size cost.
        AddConstraint<AlphaRouteConstraint>(defaults, "alpha");

#if !COMPONENTS
        AddConstraint<RegexErrorStubRouteConstraint>(defaults, "regex"); // Used to generate error message at runtime with helpful message.
        AddConstraint<RequiredRouteConstraint>(defaults, "required");
#else
        // Check if the feature is not enabled in the browser context
        if (OperatingSystem.IsBrowser() && !RegexConstraintSupport.IsEnabled)
        {
            AddConstraint<RegexErrorStubRouteConstraint>(defaults, "regex"); // Used to generate error message at runtime with helpful message.
        }
#endif

        // Files
        AddConstraint<FileNameRouteConstraint>(defaults, "file");
        AddConstraint<NonFileNameRouteConstraint>(defaults, "nonfile");

        return defaults;
    }

    /// <summary>
    /// Adds or overwrites the parameter policy with the associated route pattern token.
    /// </summary>
    /// <typeparam name="T">The parameter policy type.</typeparam>
    /// <param name="token">The route token used to apply the parameter policy.</param>
    public void SetParameterPolicy<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string token) where T : IParameterPolicy
    {
        _constraintTypeMap[token] = typeof(T);
    }

    /// <summary>
    /// Adds or overwrites the parameter policy with the associated route pattern token.
    /// </summary>
    /// <param name="token">The route token used to apply the parameter policy.</param>
    /// <param name="type">The parameter policy type.</param>
    /// <exception cref="InvalidOperationException">Throws an exception if the type is not an <see cref="IParameterPolicy"/>.</exception>
    public void SetParameterPolicy(string token, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        if (!type.IsAssignableTo(typeof(IParameterPolicy)))
        {
            throw new InvalidOperationException($"{type} must implement {typeof(IParameterPolicy)}");
        }

        _constraintTypeMap[token] = type;
    }

    private static void AddConstraint<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TConstraint>(Dictionary<string, Type> constraintMap, string text) where TConstraint : IRouteConstraint
    {
        constraintMap[text] = typeof(TConstraint);
    }
}
