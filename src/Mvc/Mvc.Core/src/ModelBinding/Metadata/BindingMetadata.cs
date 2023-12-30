// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// Binding metadata details for a <see cref="ModelMetadata"/>.
/// </summary>
public class BindingMetadata
{
    private Type? _binderType;
    private DefaultModelBindingMessageProvider? _messageProvider;

    /// <summary>
    /// Gets or sets the <see cref="ModelBinding.BindingSource"/>.
    /// See <see cref="ModelMetadata.BindingSource"/>.
    /// </summary>
    public BindingSource? BindingSource { get; set; }

    /// <summary>
    /// Gets or sets the binder model name. If <c>null</c> the property or parameter name will be used.
    /// See <see cref="ModelMetadata.BinderModelName"/>.
    /// </summary>
    public string? BinderModelName { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Type"/> of the <see cref="IModelBinder"/> implementation used to bind the
    /// model. See <see cref="ModelMetadata.BinderType"/>.
    /// </summary>
    /// <remarks>
    /// Also set <see cref="BindingSource"/> if the specified <see cref="IModelBinder"/> implementation does not
    /// use values from form data, route values or the query string.
    /// </remarks>
    public Type? BinderType
    {
        get => _binderType;
        set
        {
            if (value != null && !typeof(IModelBinder).IsAssignableFrom(value))
            {
                throw new ArgumentException(
                    Resources.FormatBinderType_MustBeIModelBinder(
                        value.FullName,
                        typeof(IModelBinder).FullName),
                    nameof(value));
            }

            _binderType = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not the property can be model bound.
    /// Will be ignored if the model metadata being created does not represent a property.
    /// See <see cref="ModelMetadata.IsBindingAllowed"/>.
    /// </summary>
    public bool IsBindingAllowed { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not the request must contain a value for the model.
    /// Will be ignored if the model metadata being created does not represent a property.
    /// See <see cref="ModelMetadata.IsBindingRequired"/>.
    /// </summary>
    public bool IsBindingRequired { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not the model is read-only. Will be ignored
    /// if the model metadata being created is not a property. If <c>null</c> then
    /// <see cref="ModelMetadata.IsReadOnly"/> will be  computed based on the accessibility
    /// of the property accessor and model <see cref="Type"/>. See <see cref="ModelMetadata.IsReadOnly"/>.
    /// </summary>
    public bool? IsReadOnly { get; set; }

    /// <summary>
    /// Gets the <see cref="Metadata.DefaultModelBindingMessageProvider"/> instance. See
    /// <see cref="ModelMetadata.ModelBindingMessageProvider"/>.
    /// </summary>
    [DisallowNull]
    public DefaultModelBindingMessageProvider? ModelBindingMessageProvider
    {
        get => _messageProvider;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _messageProvider = value;
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="ModelBinding.IPropertyFilterProvider"/>.
    /// See <see cref="ModelMetadata.PropertyFilterProvider"/>.
    /// </summary>
    public IPropertyFilterProvider? PropertyFilterProvider { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ConstructorInfo"/> used to model bind and validate the model type.
    /// </summary>
    public ConstructorInfo? BoundConstructor { get; set; }
}
