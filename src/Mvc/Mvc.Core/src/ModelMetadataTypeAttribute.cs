// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// This attribute specifies the metadata class to associate with a data model class.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class ModelMetadataTypeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelMetadataTypeAttribute" /> class.
    /// </summary>
    /// <param name="type">The type of metadata class that is associated with a data model class.</param>
    public ModelMetadataTypeAttribute(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        MetadataType = type;
    }

    /// <summary>
    /// Gets the type of metadata class that is associated with a data model class.
    /// </summary>
    public Type MetadataType { get; }
}
