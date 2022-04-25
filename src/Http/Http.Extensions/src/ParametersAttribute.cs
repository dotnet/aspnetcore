// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using System;

/// <summary>
/// 
/// </summary>
[AttributeUsage(
    AttributeTargets.Parameter | AttributeTargets.Class,
    Inherited = false,
    AllowMultiple = false)]
public sealed class ParametersAttribute : Attribute
{
}
