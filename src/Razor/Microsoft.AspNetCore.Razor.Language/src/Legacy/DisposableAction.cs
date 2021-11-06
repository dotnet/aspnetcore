// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class DisposableAction : IDisposable
{
    private readonly Action _action;
    private bool _invoked;

    public DisposableAction(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        _action = action;
    }

    public void Dispose()
    {
        if (!_invoked)
        {
            _action();
            _invoked = true;
        }
    }
}
