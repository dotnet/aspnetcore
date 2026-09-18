// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Populates a typed block property from a function result.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ToolResultAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the result property name. The block property name is used when omitted.
    /// </summary>
    public string? Name { get; set; }
}
