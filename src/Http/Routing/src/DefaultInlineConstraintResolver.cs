// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

#if !COMPONENTS
/// <summary>
/// The default implementation of <see cref="IInlineConstraintResolver"/>. Resolves constraints by parsing
/// a constraint key and constraint arguments, using a map to resolve the constraint type, and calling an
/// appropriate constructor for the constraint type.
/// </summary>
public class DefaultInlineConstraintResolver : IInlineConstraintResolver
#else
internal class DefaultInlineConstraintResolver : IInlineConstraintResolver
#endif
{
    private readonly IDictionary<string, Type> _inlineConstraintMap;
    private readonly IServiceProvider _serviceProvider;

#if !COMPONENTS
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultInlineConstraintResolver"/> class.
    /// </summary>
    /// <param name="routeOptions">Accessor for <see cref="RouteOptions"/> containing the constraints of interest.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to get service arguments from.</param>
#endif
    public DefaultInlineConstraintResolver(IOptions<RouteOptions> routeOptions, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(routeOptions);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _inlineConstraintMap = routeOptions.Value.TrimmerSafeConstraintMap;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    /// <example>
    /// A typical constraint looks like the following
    /// "exampleConstraint(arg1, arg2, 12)".
    /// Here if the type registered for exampleConstraint has a single constructor with one argument,
    /// The entire string "arg1, arg2, 12" will be treated as a single argument.
    /// In all other cases arguments are split at comma.
    /// </example>
    public virtual IRouteConstraint? ResolveConstraint(string inlineConstraint)
    {
        ArgumentNullException.ThrowIfNull(inlineConstraint);

        // This will return null if the text resolves to a non-IRouteConstraint
        return ParameterPolicyActivator.ResolveParameterPolicy<IRouteConstraint>(
            _inlineConstraintMap,
            _serviceProvider,
            inlineConstraint,
            out _);
    }
}
