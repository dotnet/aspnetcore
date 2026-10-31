// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

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
