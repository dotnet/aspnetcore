// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// A context for an <see cref="IBindingMetadataProvider"/>.
/// </summary>
public class BindingMetadataProviderContext
{
    /// <summary>
    /// Creates a new <see cref="BindingMetadataProviderContext"/>.
    /// </summary>
    /// <param name="key">The <see cref="ModelMetadataIdentity"/> for the <see cref="ModelMetadata"/>.</param>
    /// <param name="attributes">The attributes for the <see cref="ModelMetadata"/>.</param>
    public BindingMetadataProviderContext(
        ModelMetadataIdentity key,
        ModelAttributes attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        Key = key;
        Attributes = attributes.Attributes;
        ParameterAttributes = attributes.ParameterAttributes;
        PropertyAttributes = attributes.PropertyAttributes;
        TypeAttributes = attributes.TypeAttributes;

        BindingMetadata = new BindingMetadata();
    }

    /// <summary>
    /// Gets the attributes.
    /// </summary>
    public IReadOnlyList<object> Attributes { get; }

    /// <summary>
    /// Gets the <see cref="ModelMetadataIdentity"/>.
    /// </summary>
    public ModelMetadataIdentity Key { get; }

    /// <summary>
    /// Gets the parameter attributes.
    /// </summary>
    public IReadOnlyList<object>? ParameterAttributes { get; }

    /// <summary>
    /// Gets the property attributes.
    /// </summary>
    public IReadOnlyList<object>? PropertyAttributes { get; }

    /// <summary>
    /// Gets the type attributes.
    /// </summary>
    public IReadOnlyList<object>? TypeAttributes { get; }

    /// <summary>
    /// Gets the <see cref="Metadata.BindingMetadata"/>.
    /// </summary>
    public BindingMetadata BindingMetadata { get; }
}
