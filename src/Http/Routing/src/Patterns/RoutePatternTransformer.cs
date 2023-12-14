// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Routing.Patterns;

/// <summary>
/// A singleton service that provides transformations on <see cref="RoutePattern"/>.
/// </summary>
public abstract class RoutePatternTransformer
{
    /// <summary>
    /// Attempts to substitute the provided <paramref name="requiredValues"/> into the provided
    /// <paramref name="original"/>.
    /// </summary>
    /// <param name="original">The original <see cref="RoutePattern"/>.</param>
    /// <param name="requiredValues">The required values to substitute.</param>
    /// <returns>
    /// A new <see cref="RoutePattern"/> if substitution succeeds, otherwise <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Substituting required values into a route pattern is intended for us with a general-purpose
    /// parameterize route specification that can match many logical endpoints. Calling
    /// <see cref="SubstituteRequiredValues(RoutePattern, object)"/> can produce a derived route pattern
    /// for each set of route values that corresponds to an endpoint.
    /// </para>
    /// <para>
    /// The substitution process considers default values and <see cref="IRouteConstraint"/> implementations
    /// when examining a required value. <see cref="SubstituteRequiredValues(RoutePattern, object)"/> will
    /// return <c>null</c> if any required value cannot be substituted.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode("This API may perform reflection on supplied parameter which may be trimmed if not referenced directly." +
        "Consider using a different overload to avoid this issue.")]
    public abstract RoutePattern? SubstituteRequiredValues(RoutePattern original, object requiredValues);

    /// <summary>
    /// Attempts to substitute the provided <paramref name="requiredValues"/> into the provided
    /// <paramref name="original"/>.
    /// </summary>
    /// <param name="original">The original <see cref="RoutePattern"/>.</param>
    /// <param name="requiredValues">The required values to substitute.</param>
    /// <returns>
    /// A new <see cref="RoutePattern"/> if substitution succeeds, otherwise <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Substituting required values into a route pattern is intended for us with a general-purpose
    /// parameterize route specification that can match many logical endpoints. Calling
    /// <see cref="SubstituteRequiredValues(RoutePattern, object)"/> can produce a derived route pattern
    /// for each set of route values that corresponds to an endpoint.
    /// </para>
    /// <para>
    /// The substitution process considers default values and <see cref="IRouteConstraint"/> implementations
    /// when examining a required value. <see cref="SubstituteRequiredValues(RoutePattern, object)"/> will
    /// return <c>null</c> if any required value cannot be substituted.
    /// </para>
    /// </remarks>
    public virtual RoutePattern? SubstituteRequiredValues(RoutePattern original, RouteValueDictionary requiredValues)
        => throw new NotSupportedException("This API is not supported.");
}
