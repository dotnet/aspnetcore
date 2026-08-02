// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using System;

/// <summary>
/// Specifies that a route handler delegate's parameter represents a structured parameter list.
/// </summary>
[AttributeUsage(
    AttributeTargets.Parameter,
    Inherited = false,
    AllowMultiple = false)]
public sealed class AsParametersAttribute : Attribute
{
}
