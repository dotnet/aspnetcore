// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// Represents a property in a <see cref="PageApplicationModel"/>.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name}, Name = {PropertyName}")]
public class PagePropertyModel : ParameterModelBase, ICommonModel
{
    /// <summary>
    /// Creates a new instance of <see cref="PagePropertyModel"/>.
    /// </summary>
    /// <param name="propertyInfo">The <see cref="PropertyInfo"/> for the underlying property.</param>
    /// <param name="attributes">Any attributes which are annotated on the property.</param>
    public PagePropertyModel(
        PropertyInfo propertyInfo,
        IReadOnlyList<object> attributes)
        : base(propertyInfo.PropertyType, attributes)
    {
        PropertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
    }

    /// <summary>
    /// Creates a new instance of <see cref="PagePropertyModel"/> from a given <see cref="PagePropertyModel"/>.
    /// </summary>
    /// <param name="other">The <see cref="PagePropertyModel"/> which needs to be copied.</param>
    public PagePropertyModel(PagePropertyModel other)
        : base(other)
    {
        ArgumentNullException.ThrowIfNull(other);

        Page = other.Page;
        BindingInfo = other.BindingInfo == null ? null : new BindingInfo(other.BindingInfo);
        PropertyInfo = other.PropertyInfo;
    }

    /// <summary>
    /// Gets or sets the <see cref="PageApplicationModel"/> this <see cref="PagePropertyModel"/> is associated with.
    /// </summary>
    public PageApplicationModel Page { get; set; } = default!;

    MemberInfo ICommonModel.MemberInfo => PropertyInfo;

    /// <summary>
    /// The <see cref="PropertyInfo"/>.
    /// </summary>
    public PropertyInfo PropertyInfo { get; }

    /// <summary>
    /// Gets or sets the property name.
    /// </summary>
    public string PropertyName
    {
        get => Name;
        set => Name = value;
    }
}
