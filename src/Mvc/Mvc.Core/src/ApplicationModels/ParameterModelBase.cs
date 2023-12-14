// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// A model type for reading and manipulation properties and parameters.
/// <para>
/// Derived instances of this type represent properties and parameters for controllers, and Razor Pages.
/// </para>
/// </summary>
public abstract class ParameterModelBase : IBindingModel
{
    /// <summary>
    /// Initializes a new instance of a <see cref="ParameterModelBase"/>.
    /// </summary>
    /// <param name="parameterType">The type.</param>
    /// <param name="attributes">The attributes.</param>
    protected ParameterModelBase(
        Type parameterType,
        IReadOnlyList<object> attributes)
    {
        ParameterType = parameterType ?? throw new ArgumentNullException(nameof(parameterType));
        Attributes = new List<object>(attributes ?? throw new ArgumentNullException(nameof(attributes)));

        Properties = new Dictionary<object, object?>();
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="other">The other instance to copy</param>
    protected ParameterModelBase(ParameterModelBase other)
    {
        ArgumentNullException.ThrowIfNull(other);

        ParameterType = other.ParameterType;
        Attributes = new List<object>(other.Attributes);
        BindingInfo = other.BindingInfo == null ? null : new BindingInfo(other.BindingInfo);
        Name = other.Name;
        Properties = new Dictionary<object, object?>(other.Properties);
    }

    /// <summary>
    /// The attributes.
    /// </summary>
    public IReadOnlyList<object> Attributes { get; }

    /// <summary>
    /// The properties.
    /// </summary>
    public IDictionary<object, object?> Properties { get; }

    /// <summary>
    /// The type.
    /// </summary>
    public Type ParameterType { get; }

    /// <summary>
    /// The name.
    /// </summary>
    public string Name { get; protected set; } = default!;

    /// <summary>
    /// The <see cref="BindingInfo"/>.
    /// </summary>
    public BindingInfo? BindingInfo { get; set; }
}
