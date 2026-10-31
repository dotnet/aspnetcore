// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// A type which is used to represent a property in a <see cref="ControllerModel"/>.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name}, Name = {PropertyName}")]
public class PropertyModel : ParameterModelBase, ICommonModel, IBindingModel
{
    /// <summary>
    /// Creates a new instance of <see cref="PropertyModel"/>.
    /// </summary>
    /// <param name="propertyInfo">The <see cref="PropertyInfo"/> for the underlying property.</param>
    /// <param name="attributes">Any attributes which are annotated on the property.</param>
    public PropertyModel(
        PropertyInfo propertyInfo,
        IReadOnlyList<object> attributes)
        : base(propertyInfo.PropertyType, attributes)
    {
        PropertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
    }

    /// <summary>
    /// Creates a new instance of <see cref="PropertyModel"/> from a given <see cref="PropertyModel"/>.
    /// </summary>
    /// <param name="other">The <see cref="PropertyModel"/> which needs to be copied.</param>
    public PropertyModel(PropertyModel other)
        : base(other)
    {
        ArgumentNullException.ThrowIfNull(other);

        Controller = other.Controller;
        BindingInfo = other.BindingInfo == null ? null : new BindingInfo(other.BindingInfo);
        PropertyInfo = other.PropertyInfo;
    }

    /// <summary>
    /// Gets or sets the <see cref="ControllerModel"/> this <see cref="PropertyModel"/> is associated with.
    /// </summary>
    public ControllerModel Controller { get; set; } = default!;

    MemberInfo ICommonModel.MemberInfo => PropertyInfo;

    /// <inheritdoc/>
    public new IDictionary<object, object?> Properties => base.Properties;

    /// <inheritdoc/>
    public new IReadOnlyList<object> Attributes => base.Attributes;

    /// <summary>
    /// The <see cref="PropertyInfo"/>.
    /// </summary>
    public PropertyInfo PropertyInfo { get; }

    /// <summary>
    /// The name of the property.
    /// </summary>
    public string PropertyName
    {
        get => Name;
        set => Name = value;
    }
}
