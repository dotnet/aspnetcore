// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Indicates that the type and any derived types that this attribute is applied to
/// are considered a controller by the default controller discovery mechanism, unless
/// <see cref="NonControllerAttribute"/> is applied to any type in the hierarchy.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class ControllerAttribute : Attribute
{
}
