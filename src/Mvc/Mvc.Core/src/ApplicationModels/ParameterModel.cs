// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// A type that represents a parameter.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name}, Name = {ParameterName}")]
public class ParameterModel : ParameterModelBase, ICommonModel
{
    /// <summary>
    /// Initializes a new <see cref="ParameterModel"/>.
    /// </summary>
    /// <param name="parameterInfo">The parameter info.</param>
    /// <param name="attributes">The attributes.</param>
    public ParameterModel(
        ParameterInfo parameterInfo,
        IReadOnlyList<object> attributes)
        : base(parameterInfo.ParameterType, attributes)
    {
        ParameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));
    }

    /// <summary>
    /// Initializes a new <see cref="ParameterModel"/>.
    /// </summary>
    /// <param name="other">The parameter model to copy.</param>
    public ParameterModel(ParameterModel other)
        : base(other)
    {
        ArgumentNullException.ThrowIfNull(other);

        Action = other.Action;
        ParameterInfo = other.ParameterInfo;
    }

    /// <summary>
    /// The <see cref="ActionModel"/>.
    /// </summary>
    public ActionModel Action { get; set; } = default!;

    /// <summary>
    /// The properties.
    /// </summary>
    public new IDictionary<object, object?> Properties => base.Properties;

    /// <summary>
    /// The attributes.
    /// </summary>
    public new IReadOnlyList<object> Attributes => base.Attributes;

    MemberInfo ICommonModel.MemberInfo => ParameterInfo.Member;

    /// <summary>
    /// The <see cref="ParameterInfo"/>.
    /// </summary>
    public ParameterInfo ParameterInfo { get; }

    /// <summary>
    /// The parameter name.
    /// </summary>
    public string ParameterName
    {
        get => Name;
        set => Name = value;
    }

    /// <summary>
    /// The display name.
    /// </summary>
    public string DisplayName
    {
        get
        {
            var parameterTypeName = TypeNameHelper.GetTypeDisplayName(ParameterInfo.ParameterType, fullName: false);
            return $"{parameterTypeName} {ParameterName}";
        }
    }
}
