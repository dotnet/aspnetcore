// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests
{
    public class LibuvConstantsTests
    {
        [Fact]
        public void IsConnectionResetReturnsTrueForExpectedLibuvErrorConstants()
        {
            // All the below constants are defined on all supported platforms (Windows, Linux, macOS)
            Assert.True(LibuvConstants.IsConnectionReset(LibuvConstants.ECONNRESET.Value));
            Assert.True(LibuvConstants.IsConnectionReset(LibuvConstants.EPIPE.Value));
            Assert.True(LibuvConstants.IsConnectionReset(LibuvConstants.ENOTCONN.Value));
            Assert.True(LibuvConstants.IsConnectionReset(LibuvConstants.EINVAL.Value));

            // All libuv error constants are negative on all platforms.
            Assert.False(LibuvConstants.IsConnectionReset(0));
        }
    }
}
