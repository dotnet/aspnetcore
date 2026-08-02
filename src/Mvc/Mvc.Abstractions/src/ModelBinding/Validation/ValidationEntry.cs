// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// Contains data needed for validating a child entry of a model object. See <see cref="IValidationStrategy"/>.
/// </summary>
public struct ValidationEntry
{
    private object? _model;
    private Func<object?>? _modelAccessor;

    /// <summary>
    /// Creates a new <see cref="ValidationEntry"/>.
    /// </summary>
    /// <param name="metadata">The <see cref="ModelMetadata"/> associated with <paramref name="model"/>.</param>
    /// <param name="key">The model prefix associated with <paramref name="model"/>.</param>
    /// <param name="model">The model object.</param>
    public ValidationEntry(ModelMetadata metadata, string key, object? model)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(key);

        Metadata = metadata;
        Key = key;
        _model = model;
        _modelAccessor = null;
    }

    /// <summary>
    /// Creates a new <see cref="ValidationEntry"/>.
    /// </summary>
    /// <param name="metadata">The <see cref="ModelMetadata"/> associated with the <see cref="Model"/>.</param>
    /// <param name="key">The model prefix associated with the <see cref="Model"/>.</param>
    /// <param name="modelAccessor">A delegate that will return the <see cref="Model"/>.</param>
    public ValidationEntry(ModelMetadata metadata, string key, Func<object?> modelAccessor)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(modelAccessor);

        Metadata = metadata;
        Key = key;
        _model = null;
        _modelAccessor = modelAccessor;
    }

    /// <summary>
    /// The model prefix associated with <see cref="Model"/>.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// The <see cref="ModelMetadata"/> associated with <see cref="Model"/>.
    /// </summary>
    public ModelMetadata Metadata { get; }

    /// <summary>
    /// The model object.
    /// </summary>
    public object? Model
    {
        get
        {
            if (_modelAccessor != null)
            {
                _model = _modelAccessor();
                _modelAccessor = null;
            }

            return _model;
        }
    }
}
