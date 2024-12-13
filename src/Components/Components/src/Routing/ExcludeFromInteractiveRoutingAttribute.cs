// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// When applied to a page component, indicates that the interactive <see cref="Router"/> component should
/// ignore that page. This means that navigations to the page will not be resolved by interactive routing,
/// but instead will cause a full page reload.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ExcludeFromInteractiveRoutingAttribute : Attribute
{
}
