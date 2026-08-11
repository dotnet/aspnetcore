// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.JSInterop.Infrastructure;

internal readonly record struct JSInvokableMethodInfo(
    string? AssemblyName,
    Type? TargetType,
    string Identifier)
{
    public bool IsStatic => TargetType is null;
}
