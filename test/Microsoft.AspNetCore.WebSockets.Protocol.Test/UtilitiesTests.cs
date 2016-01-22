// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.WebSockets.Protocol.Test
{
    public class UtilitiesTests
    {
        [Fact]
        public void MaskDataRoundTrips()
        {
            byte[] data = Encoding.UTF8.GetBytes("Hello World");
            byte[] orriginal = Encoding.UTF8.GetBytes("Hello World");
            Utilities.MaskInPlace(16843009, new ArraySegment<byte>(data));
            Utilities.MaskInPlace(16843009, new ArraySegment<byte>(data));
            Assert.Equal(orriginal, data);
        }
    }
}
