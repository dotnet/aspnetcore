// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// An interface which is used to represent a something with a <see cref="BindingInfo"/>.
/// </summary>
public interface IBindingModel
{
    /// <summary>
    /// The <see cref="BindingInfo"/>.
    /// </summary>
    BindingInfo? BindingInfo { get; set; }
}
