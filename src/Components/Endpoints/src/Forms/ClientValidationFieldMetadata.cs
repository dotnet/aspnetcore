// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Components.Endpoints.Forms;

// Per-field reflection results. Culture-independent; localized text is resolved per call.
// At most one of ResourceDisplayAttribute and LiteralDisplayName is non-null; both null means
// the field property has no display attribute.
internal readonly struct ClientValidationFieldMetadata(
    string propertyName,
    ValidationAttribute[] validationAttributes,
    Type declaringType,
    DisplayAttribute? resourceDisplayAttribute,
    string? literalDisplayName)
{
    public string PropertyName { get; } = propertyName;

    public ValidationAttribute[] ValidationAttributes { get; } = validationAttributes;

    // Type that declares the property. Differs from the runtime container type for inherited
    // properties. Used for localizer resolution so the client mirrors the server, which resolves
    // the localizer, message key, and display name from the declaring type.
    public Type DeclaringType { get; } = declaringType;

    // [Display(Name=..., ResourceType=...)].
    public DisplayAttribute? ResourceDisplayAttribute { get; } = resourceDisplayAttribute;

    // [Display(Name="X")] (no ResourceType) or [DisplayName("X")].
    public string? LiteralDisplayName { get; } = literalDisplayName;
}
