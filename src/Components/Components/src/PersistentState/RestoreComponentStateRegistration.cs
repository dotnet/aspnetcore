// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

internal readonly struct RestoreComponentStateRegistration(Action callback)
{
    public Action Callback { get; } = callback;
}
