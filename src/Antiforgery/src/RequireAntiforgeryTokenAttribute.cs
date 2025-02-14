// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// An attribute that can be used to indicate whether the antiforgery token must be validated.
/// </summary>
/// <param name="required">A value indicating whether the antiforgery token should be validated.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireAntiforgeryTokenAttribute(bool required = true) : Attribute, IAntiforgeryMetadata
{
    /// <summary>
    /// Gets or sets a value indicating whether the antiforgery token should be validated.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="true"/>; <see langword="false"/> indicates that
    /// the validation check for the antiforgery token can be avoided.
    /// </remarks>
    public bool RequiresValidation { get; } = required;
}
