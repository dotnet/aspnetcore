// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Indicates that a RenderFragment parameter should be executed during serialization
/// if it was not invoked during prerendering (e.g., due to conditional rendering).
/// Apply this to individual <see cref="RenderFragment"/> parameters that should be
/// serialized regardless of whether the component rendered them.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class SerializationExecutionPolicyAttribute : Attribute
{
}
