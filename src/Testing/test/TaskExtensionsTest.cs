// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class TaskExtensionsTest
    {
        [Fact]
        public async Task TimeoutAfterTest()
        {
            var cts = new CancellationTokenSource();
            await Assert.ThrowsAsync<TimeoutException>(async () => await Task.Delay(30000, cts.Token).TimeoutAfter(TimeSpan.FromMilliseconds(50)));
            cts.Cancel();
        }
    }
}
