// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ToolParameterAttribute : Attribute
{
    public string? Name { get; set; }
}
