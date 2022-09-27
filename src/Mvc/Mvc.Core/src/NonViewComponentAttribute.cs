// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Indicates that the type and any derived types that this attribute is applied to
/// is not considered a view component by the default view component discovery mechanism.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class NonViewComponentAttribute : Attribute
{
}
