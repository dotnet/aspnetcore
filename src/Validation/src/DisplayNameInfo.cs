// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Validation.Localization;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Resolves the display name of a validated member (property, parameter, or type).
/// Each <see cref="ValidatablePropertyInfo"/>, <see cref="ValidatableParameterInfo"/>, and
/// <see cref="ValidatableTypeInfo"/> may carry a single <see cref="DisplayNameInfo"/> instance
/// that encapsulates the strategy for producing its display name at validation time.
/// </summary>
/// <remarks>
/// <para>
/// Implementations encode a single source of the display name (for example, a literal value from
/// <see cref="DisplayAttribute.Name"/>, a static resource accessor for
/// <c>[Display(ResourceType = ..., Name = ...)]</c>, or a custom strategy). The validation
/// pipeline calls <see cref="GetDisplayName(ValidateContext, string, Type?)"/> once per
/// member validation and uses the result, falling back to the CLR member name when the
/// implementation returns <see langword="null"/>.
/// </para>
/// <para>
/// Implementations may participate in localization by inspecting
/// <see cref="ValidationOptions.Localizer"/> on
/// <see cref="ValidateContext.ValidationOptions"/>. Implementations that source their value
/// from a static resource (the <see cref="DisplayAttribute.ResourceType"/> path) typically
/// bypass <see cref="IValidationLocalizer"/> because the resource lookup is the canonical
/// source for the localized name.
/// </para>
/// </remarks>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public abstract class DisplayNameInfo
{
    /// <summary>
    /// Resolves the display name to use when reporting validation errors for the member.
    /// </summary>
    /// <param name="context">The current validation context. Provides access to
    /// <see cref="ValidationOptions.Localizer"/> for implementations that delegate to
    /// <see cref="IValidationLocalizer"/>.</param>
    /// <param name="memberName">The CLR member name (property name, parameter name, or
    /// type name) being validated. Implementations may use this as a fallback display name
    /// or as a localization lookup key.</param>
    /// <param name="type">The type that declares the member for property-level validation,
    /// the validated type itself for type-level validation, or <see langword="null"/>
    /// for parameter validation.</param>
    /// <returns>The display name for the member, or <see langword="null"/> when no value can
    /// be produced. The validation pipeline falls back to <paramref name="memberName"/> in
    /// the <see langword="null"/> case.</returns>
    public abstract string? GetDisplayName(ValidateContext context, string memberName, Type? type);
}
