// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// Enumeration for the kinds of <see cref="ModelMetadata"/>
/// </summary>
public enum ModelMetadataKind
{
    /// <summary>
    /// Used for <see cref="ModelMetadata"/> for a <see cref="System.Type"/>.
    /// </summary>
    Type,

    /// <summary>
    /// Used for <see cref="ModelMetadata"/> for a property.
    /// </summary>
    Property,

    /// <summary>
    /// Used for <see cref="ModelMetadata"/> for a parameter.
    /// </summary>
    Parameter,

    /// <summary>
    /// <see cref="ModelMetadata"/> for a constructor.
    /// </summary>
    Constructor,
}
