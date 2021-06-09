// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
