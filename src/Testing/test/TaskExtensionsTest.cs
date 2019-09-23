// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class TaskExtensionsTest
    {
        [Fact]
        public async Task TimeoutAfterTest()
        {
            await Assert.ThrowsAsync<TimeoutException>(async () => await Task.Delay(1000).TimeoutAfter(TimeSpan.FromMilliseconds(50)));
        }
    }
}
