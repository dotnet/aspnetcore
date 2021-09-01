// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    [Retry]
    public class RetryTest
    {
        private static int _retryFailsUntil3 = 0;

        [Fact]
        public void RetryFailsUntil3()
        {
            _retryFailsUntil3++;
            if (_retryFailsUntil3 != 2) throw new Exception("NOOOOOOOO");
        }

        private static int _canOverrideRetries = 0;

        [Fact]
        [Retry(5)]
        public void CanOverrideRetries()
        {
            _canOverrideRetries++;
            if (_canOverrideRetries != 5) throw new Exception("NOOOOOOOO");
        }
    }
}
