// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal static class CancellationTokenUtils
{
    // Similar to CreateLinkedTokenSource except it will not allocate a new internal LinkedCancellationTokenSource in the case where
    // one of the tokens passed in isn't cancellable.
    // Returns a disposable only when an actual LinkedTokenSource is created.
    internal static IDisposable? CreateLinkedToken(CancellationToken token1, CancellationToken token2, out CancellationToken linkedToken)
    {
        if (!token1.CanBeCanceled)
        {
            linkedToken = token2;
            return null;
        }
        else if (!token2.CanBeCanceled)
        {
            linkedToken = token1;
            return null;
        }
        else
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(token1, token2);
            linkedToken = cts.Token;
            return cts;
        }
    }
}
