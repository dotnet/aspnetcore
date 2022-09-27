// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// Specifies the default status code associated with an <see cref="ActionResult"/>.
/// </summary>
/// <remarks>
/// This attribute is informational only and does not have any runtime effects.
/// Applying the attribute on a class indicates that the <see cref="ActionResult"/>
/// represented by that class uses a particular status code by default. Applying the
/// attribute to a method indicates that the <see cref="ActionResult"/> returned by the
/// method uses that status code by default. The later is helpful in scenarios where we
/// need to specify that a method modifies the status code that an <see cref="ActionResult"/>
/// uses by default in its logic or for specifying the status code for consumption in
/// the API analyzers.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class DefaultStatusCodeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="DefaultStatusCodeAttribute"/>.
    /// </summary>
    /// <param name="statusCode">The default status code.</param>
    public DefaultStatusCodeAttribute(int statusCode)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the default status code.
    /// </summary>
    public int StatusCode { get; }
}
