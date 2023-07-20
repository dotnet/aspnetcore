// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Metadata which specifies custom form limits for model binding..
/// </summary>
public interface IBindingLimitsMetadata
{
    /// <summary>
    /// Gets the maximum number of collection items to allow during model binding.
    /// </summary>
    public int? MaxModelBindingCollectionSize { get; }

    /// <summary>
    /// Gets the maximum depth of the model object graph that will be bound.
    /// </summary>
    public int? MaxModelBindingRecursionDepth { get; }
}
