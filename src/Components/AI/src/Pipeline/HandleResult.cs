// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

internal readonly struct HandleResult
{
    internal enum ResultKind { Pass, Update, Complete }

    internal ResultKind Kind { get; }

    private HandleResult(ResultKind kind)
    {
        Kind = kind;
    }

    internal static HandleResult Pass() => new(ResultKind.Pass);
    internal static HandleResult Update() => new(ResultKind.Update);
    internal static HandleResult Complete() => new(ResultKind.Complete);
}
