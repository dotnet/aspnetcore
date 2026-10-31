// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// An <see cref="IValidationMetadataProvider"/> which configures <see cref="ModelMetadata.ValidateChildren"/> to
/// <c>false</c> for matching types.
/// </summary>
public class SuppressChildValidationMetadataProvider : IValidationMetadataProvider
{
    /// <summary>
    /// Creates a new <see cref="SuppressChildValidationMetadataProvider"/> for the given <paramref name="type"/>.
    /// </summary>
    /// <param name="type">
    /// The <see cref="Type"/>. This <see cref="Type"/> and all assignable values will have
    /// <see cref="ModelMetadata.ValidateChildren"/> set to <c>false</c>.
    /// </param>
    public SuppressChildValidationMetadataProvider(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        Type = type;
    }

    /// <summary>
    /// Creates a new <see cref="SuppressChildValidationMetadataProvider"/> for the given <paramref name="fullTypeName"/>.
    /// </summary>
    /// <param name="fullTypeName">
    /// The type full name. This type and all of its subclasses will have
    /// <see cref="ModelMetadata.ValidateChildren"/> set to <c>false</c>.
    /// </param>
    public SuppressChildValidationMetadataProvider(string fullTypeName)
    {
        ArgumentNullException.ThrowIfNull(fullTypeName);

        FullTypeName = fullTypeName;
    }

    /// <summary>
    /// Gets the <see cref="Type"/> for which to suppress validation of children.
    /// </summary>
    public Type? Type { get; }

    /// <summary>
    /// Gets the full name of a type for which to suppress validation of children.
    /// </summary>
    public string? FullTypeName { get; }

    /// <inheritdoc />
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (Type != null)
        {
            if (Type.IsAssignableFrom(context.Key.ModelType))
            {
                context.ValidationMetadata.ValidateChildren = false;
            }

            return;
        }

        if (FullTypeName != null)
        {
            if (IsMatchingName(context.Key.ModelType))
            {
                context.ValidationMetadata.ValidateChildren = false;
            }

            return;
        }

        Debug.Fail("We shouldn't get here.");
    }

    private bool IsMatchingName(Type? type)
    {
        Debug.Assert(FullTypeName != null);

        if (type == null)
        {
            return false;
        }

        if (string.Equals(type.FullName, FullTypeName, StringComparison.Ordinal))
        {
            return true;
        }

        return IsMatchingName(type.BaseType);
    }
}
