// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class ThrowingWriteOnlyStreamTests
    {
        [Fact]
        public async Task ThrowsOnWrite()
        {
            var ex = new Exception("my error");
            var stream = new ThrowingWriteOnlyStream(ex);

            Assert.True(stream.CanWrite);
            Assert.False(stream.CanRead);
            Assert.False(stream.CanSeek);
            Assert.False(stream.CanTimeout);
            Assert.Same(ex, Assert.Throws<Exception>(() => stream.Write(new byte[1], 0, 1)));
            Assert.Same(ex, await Assert.ThrowsAsync<Exception>(() => stream.WriteAsync(new byte[1], 0, 1)));
            Assert.Same(ex, Assert.Throws<Exception>(() => stream.Flush()));
            Assert.Same(ex, await Assert.ThrowsAsync<Exception>(() => stream.FlushAsync()));
        }
    }
}
