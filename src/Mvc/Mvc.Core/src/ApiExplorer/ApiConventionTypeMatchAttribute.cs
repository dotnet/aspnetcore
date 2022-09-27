// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

/// <summary>
/// Determines the matching behavior an API convention parameter by type.
/// <see cref="ApiConventionTypeMatchBehavior"/> for supported options.
/// <seealso cref="ApiConventionTypeAttribute"/>.
/// </summary>
/// <remarks>
/// <see cref="ApiConventionTypeMatchBehavior.AssignableFrom"/> is used if no value for this
/// attribute is specified on a convention parameter.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class ApiConventionTypeMatchAttribute : Attribute
{
    /// <summary>
    /// Initialzes a new instance of <see cref="ApiConventionTypeMatchAttribute"/> with the specified <paramref name="matchBehavior"/>.
    /// </summary>
    /// <param name="matchBehavior">The <see cref="ApiConventionTypeMatchBehavior"/>.</param>
    public ApiConventionTypeMatchAttribute(ApiConventionTypeMatchBehavior matchBehavior)
    {
        MatchBehavior = matchBehavior;
    }

    /// <summary>
    /// Gets the <see cref="ApiConventionTypeMatchBehavior"/>.
    /// </summary>
    public ApiConventionTypeMatchBehavior MatchBehavior { get; }
}
