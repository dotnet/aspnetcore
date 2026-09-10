// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Indicates that a component parameter accepts a static asset path that the Razor compiler can expand.
/// </summary>
/// <remarks>
/// This attribute is valid only on properties that are also marked with <see cref="ParameterAttribute"/>.
/// </remarks>
/// <example>
/// <code>
/// [Parameter]
/// [AssetPath]
/// public string? Source { get; set; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class AssetPathAttribute : Attribute
{
}
