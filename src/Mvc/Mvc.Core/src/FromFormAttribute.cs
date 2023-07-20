// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Specifies that a parameter or property should be bound using form-data in the request body.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FromFormAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider, IFromFormMetadata, IBindingLimitsMetadata
{
    private int? _maxModelBindingCollectionSize;
    private int? _maxModelBindingRecursionDepth;

    /// <inheritdoc />
    public BindingSource BindingSource => BindingSource.Form;

    /// <inheritdoc />
    public string? Name { get; set; }

    /// <summary>
    /// Gets the maximum number of collection items to allow during model binding.
    /// </summary>
    public int MaxModelBindingCollectionSize
    {
        get { return _maxModelBindingCollectionSize ?? 0; }
        set { _maxModelBindingCollectionSize = value; }
    }

    /// <summary>
    /// Gets the maximum depth of the model object graph that will be bound.
    /// </summary>
    public int MaxModelBindingRecursionDepth
    {
        get { return _maxModelBindingRecursionDepth ?? 0; }
        set { _maxModelBindingRecursionDepth = value; }
    }

    /// <inheritdoc />
    int? IBindingLimitsMetadata.MaxModelBindingCollectionSize => _maxModelBindingCollectionSize;

    /// <inheritdoc />
    int? IBindingLimitsMetadata.MaxModelBindingRecursionDepth => _maxModelBindingRecursionDepth;
}
