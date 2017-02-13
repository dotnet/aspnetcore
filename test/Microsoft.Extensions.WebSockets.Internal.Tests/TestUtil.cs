// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.WebSockets.Internal.Tests
{
    internal static class TestUtil
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);

        public static CancellationToken CreateTimeoutToken() => CreateTimeoutToken(DefaultTimeout);

        public static CancellationToken CreateTimeoutToken(TimeSpan timeout)
        {
            if (Debugger.IsAttached)
            {
                return CancellationToken.None;
            }
            else
            {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(timeout);
                return cts.Token;
            }
        }
    }
}
