// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Supports configuring the behavior of form mapping in a minimal API.
/// </summary>
public class FormMappingOptionsMetadata(int? maxCollectionSize = null, int? maxRecursionDepth = null, int? maxKeySize = null)
{
    /// <summary>
    /// Gets or sets the maximum number of elements allowed in a form collection.
    /// </summary>
    public int? MaxCollectionSize { get; } = maxCollectionSize;

    /// <summary>
    /// Gets or sets the maximum depth allowed when recursively mapping form data.
    /// </summary>
    public int? MaxRecursionDepth { get; } = maxRecursionDepth;

    /// <summary>
    /// Gets or sets the maximum size of the buffer used to read form data keys.
    /// </summary>
    public int? MaxKeySize { get; } = maxKeySize;
}
