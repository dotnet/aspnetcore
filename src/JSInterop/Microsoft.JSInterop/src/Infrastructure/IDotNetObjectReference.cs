// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.JSInterop.Infrastructure;

internal interface IDotNetObjectReference : IDisposable
{
    object Value { get; }

    [DynamicallyAccessedMembers(JSInvokable)]
    Type Type { get; }
}
