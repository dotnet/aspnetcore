// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

/// <inheritdoc />
/// <typeparam name="T">The type of metadata class that is associated with a data model class.</typeparam>
/// <remarks>
/// This is a derived generic variant of the <see cref="ModelMetadataTypeAttribute"/>
/// which does not allow multiple instances on a single target.
/// Ensure that only one instance of either attribute is provided on the target.
/// </remarks>
public class ModelMetadataTypeAttribute<T> : ModelMetadataTypeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelMetadataTypeAttribute" /> class.
    /// </summary>
    public ModelMetadataTypeAttribute() : base(typeof(T)) { }
}
