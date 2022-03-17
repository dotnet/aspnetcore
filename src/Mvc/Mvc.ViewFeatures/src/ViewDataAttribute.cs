// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Properties decorated with <see cref="ViewDataAttribute"/> will have their values stored in
/// and loaded from the <see cref="ViewDataDictionary"/>. <see cref="ViewDataDictionary"/>
/// is supported on properties of Controllers, and Razor Page handlers.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class ViewDataAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the key used to get or add the property from value from <see cref="ViewDataDictionary"/>.
    /// When unspecified, the key is the property name.
    /// </summary>
    public string? Key { get; set; }
}
