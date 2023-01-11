// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// A context for and <see cref="IDisplayMetadataProvider"/>.
/// </summary>
public class DisplayMetadataProviderContext
{
    /// <summary>
    /// Creates a new <see cref="DisplayMetadataProviderContext"/>.
    /// </summary>
    /// <param name="key">The <see cref="ModelMetadataIdentity"/> for the <see cref="ModelMetadata"/>.</param>
    /// <param name="attributes">The attributes for the <see cref="ModelMetadata"/>.</param>
    public DisplayMetadataProviderContext(
        ModelMetadataIdentity key,
        ModelAttributes attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        Key = key;
        Attributes = attributes.Attributes;
        PropertyAttributes = attributes.PropertyAttributes;
        TypeAttributes = attributes.TypeAttributes;

        DisplayMetadata = new DisplayMetadata();
    }

    /// <summary>
    /// Gets the attributes.
    /// </summary>
    public IReadOnlyList<object> Attributes { get; }

    /// <summary>
    /// Gets the <see cref="Metadata.DisplayMetadata"/>.
    /// </summary>
    public DisplayMetadata DisplayMetadata { get; }

    /// <summary>
    /// Gets the <see cref="ModelMetadataIdentity"/>.
    /// </summary>
    public ModelMetadataIdentity Key { get; }

    /// <summary>
    /// Gets the property attributes.
    /// </summary>
    public IReadOnlyList<object>? PropertyAttributes { get; }

    /// <summary>
    /// Gets the type attributes.
    /// </summary>
    public IReadOnlyList<object>? TypeAttributes { get; }
}
