// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// A model type for reading and manipulation properties and parameters representing a Page Parameter.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name}, Name = {ParameterName}")]
public class PageParameterModel : ParameterModelBase, ICommonModel, IBindingModel
{
    /// <summary>
    /// Initializes a new instance of a <see cref="PageParameterModel"/>.
    /// </summary>
    /// <param name="parameterInfo">The parameter info.</param>
    /// <param name="attributes">The attributes.</param>
    public PageParameterModel(
        ParameterInfo parameterInfo,
        IReadOnlyList<object> attributes)
        : base(parameterInfo.ParameterType, attributes)
    {
        ArgumentNullException.ThrowIfNull(parameterInfo);
        ArgumentNullException.ThrowIfNull(attributes);

        ParameterInfo = parameterInfo;
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="other">The model to copy.</param>
    public PageParameterModel(PageParameterModel other)
        : base(other)
    {
        ArgumentNullException.ThrowIfNull(other);

        Handler = other.Handler;
        ParameterInfo = other.ParameterInfo;
    }

    /// <summary>
    /// The <see cref="PageHandlerModel"/>.
    /// </summary>
    public PageHandlerModel Handler { get; set; } = default!;

    MemberInfo ICommonModel.MemberInfo => ParameterInfo.Member;

    /// <summary>
    /// The <see cref="ParameterInfo"/>.
    /// </summary>
    public ParameterInfo ParameterInfo { get; }

    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    public string ParameterName
    {
        get => Name;
        set => Name = value;
    }
}
