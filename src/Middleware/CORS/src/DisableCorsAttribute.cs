// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Microsoft.AspNetCore.Cors;

/// <inheritdoc />
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class DisableCorsAttribute : Attribute, IDisableCorsAttribute
{
}
