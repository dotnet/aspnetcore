// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// The list of view component types in an MVC application.The <see cref="ViewComponentFeature"/> can be populated
/// using the <see cref="ApplicationPartManager"/> that is available during startup at <see cref="IMvcBuilder.PartManager"/>
/// and <see cref="IMvcCoreBuilder.PartManager"/> or at a later stage by requiring the <see cref="ApplicationPartManager"/>
/// as a dependency in a component.
/// </summary>
public class ViewComponentFeature
{
    /// <summary>
    /// Gets the list of view component types in an MVC application.
    /// </summary>
    public IList<TypeInfo> ViewComponents { get; } = new List<TypeInfo>();
}
