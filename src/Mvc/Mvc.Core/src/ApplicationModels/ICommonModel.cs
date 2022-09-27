// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// ICommonModel interface.
/// </summary>
public interface ICommonModel : IPropertyModel
{
    /// <summary>
    /// The attributes.
    /// </summary>
    IReadOnlyList<object> Attributes { get; }

    /// <summary>
    /// The MemberInfo.
    /// </summary>
    MemberInfo MemberInfo { get; }

    /// <summary>
    /// The name.
    /// </summary>
    string Name { get; }
}
