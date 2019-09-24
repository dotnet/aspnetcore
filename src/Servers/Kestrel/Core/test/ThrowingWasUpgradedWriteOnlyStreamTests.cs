// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class ThrowingWasUpgradedWriteOnlyStreamTests
    {
        [Fact]
        public async Task ThrowsOnWrite()
        {
            var stream = new ThrowingWasUpgradedWriteOnlyStream();

            Assert.True(stream.CanWrite);
            Assert.False(stream.CanRead);
            Assert.False(stream.CanSeek);
            Assert.False(stream.CanTimeout);
            Assert.Equal(CoreStrings.ResponseStreamWasUpgraded, Assert.Throws<InvalidOperationException>(() => stream.Write(new byte[1], 0, 1)).Message);
            Assert.Equal(CoreStrings.ResponseStreamWasUpgraded, (await Assert.ThrowsAsync<InvalidOperationException>(() => stream.WriteAsync(new byte[1], 0, 1))).Message);
            Assert.Equal(CoreStrings.ResponseStreamWasUpgraded, Assert.Throws<InvalidOperationException>(() => stream.Flush()).Message);
            Assert.Equal(CoreStrings.ResponseStreamWasUpgraded, (await Assert.ThrowsAsync<InvalidOperationException>(() => stream.FlushAsync())).Message);
        }
    }
}
