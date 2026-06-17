// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Indicates that the associated property should have a value injected from the
/// service provider during initialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class InjectAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the object that specifies the key of the service to inject.
    /// </summary>
    public object? Key { get; init; }
}
