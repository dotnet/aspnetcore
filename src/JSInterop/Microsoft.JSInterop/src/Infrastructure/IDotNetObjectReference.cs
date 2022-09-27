// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.JSInterop.Infrastructure;

internal interface IDotNetObjectReference : IDisposable
{
    object Value { get; }
}
