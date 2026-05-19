// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Contains the result of model binding.
/// </summary>
public readonly struct ModelBindingResult : IEquatable<ModelBindingResult>
{
    /// <summary>
    /// Creates a <see cref="ModelBindingResult"/> representing a failed model binding operation.
    /// </summary>
    /// <returns>A <see cref="ModelBindingResult"/> representing a failed model binding operation.</returns>
    public static ModelBindingResult Failed()
    {
        return new ModelBindingResult(model: null, isModelSet: false);
    }

    /// <summary>
    /// Creates a <see cref="ModelBindingResult"/> representing a successful model binding operation.
    /// </summary>
    /// <param name="model">The model value. May be <c>null.</c></param>
    /// <returns>A <see cref="ModelBindingResult"/> representing a successful model bind.</returns>
    public static ModelBindingResult Success(object? model)
    {
        return new ModelBindingResult(model, isModelSet: true);
    }

    private ModelBindingResult(object? model, bool isModelSet)
    {
        Model = model;
        IsModelSet = isModelSet;
    }

    /// <summary>
    /// Gets the model associated with this context.
    /// </summary>
    public object? Model { get; }

    /// <summary>
    /// <para>
    /// Gets a value indicating whether or not the <see cref="Model"/> value has been set.
    /// </para>
    /// <para>
    /// This property can be used to distinguish between a model binder which does not find a value and
    /// the case where a model binder sets the <c>null</c> value.
    /// </para>
    /// </summary>
    public bool IsModelSet { get; }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        var other = obj as ModelBindingResult?;
        if (other == null)
        {
            return false;
        }
        else
        {
            return Equals(other.Value);
        }
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(IsModelSet, Model);
    }

    /// <inheritdoc />
    public bool Equals(ModelBindingResult other)
    {
        return
            IsModelSet == other.IsModelSet &&
            object.Equals(Model, other.Model);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (IsModelSet)
        {
            return $"Success '{Model}'";
        }
        else
        {
            return "Failed";
        }
    }

    /// <summary>
    /// Compares <see cref="ModelBindingResult"/> objects for equality.
    /// </summary>
    /// <param name="x">A <see cref="ModelBindingResult"/>.</param>
    /// <param name="y">A <see cref="ModelBindingResult"/>.</param>
    /// <returns><c>true</c> if the objects are equal, otherwise <c>false</c>.</returns>
    public static bool operator ==(ModelBindingResult x, ModelBindingResult y)
    {
        return x.Equals(y);
    }

    /// <summary>
    /// Compares <see cref="ModelBindingResult"/> objects for inequality.
    /// </summary>
    /// <param name="x">A <see cref="ModelBindingResult"/>.</param>
    /// <param name="y">A <see cref="ModelBindingResult"/>.</param>
    /// <returns><c>true</c> if the objects are not equal, otherwise <c>false</c>.</returns>
    public static bool operator !=(ModelBindingResult x, ModelBindingResult y)
    {
        return !x.Equals(y);
    }
}
