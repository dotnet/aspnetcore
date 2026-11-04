// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting;

[Retry]
public class RetryTest
{
    private static int _retryFailsUntil3 = 0;
    private bool _wasInvokedPreviously;

    [Fact]
    public void RetryFailsUntil3()
    {
        // Validate that we get a new class instance per retry
        Assert.False(_wasInvokedPreviously);
        _wasInvokedPreviously = true;

        _retryFailsUntil3++;
        if (_retryFailsUntil3 != 2)
        {
            throw new Exception($"NOOOOOOOO [retry count={_retryFailsUntil3}]");
        }
    }

    private static int _canOverrideRetries = 0;

    [Fact]
    [Retry(5)]
    public void CanOverrideRetries()
    {
        // Validate that we get a new class instance per retry
        Assert.False(_wasInvokedPreviously);
        _wasInvokedPreviously = true;

        _canOverrideRetries++;
        if (_canOverrideRetries != 5)
        {
            throw new Exception($"NOOOOOOOO [retry count={_canOverrideRetries}]");
        }
    }
}
