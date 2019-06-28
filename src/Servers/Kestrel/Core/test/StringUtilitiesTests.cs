// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class StringUtilitiesTests
    {
        [Theory]
        [InlineData(uint.MinValue)]
        [InlineData(0xF)]
        [InlineData(0xA)]
        [InlineData(0xFF)]
        [InlineData(0xFFC59)]
        [InlineData(uint.MaxValue)]
        public void ConvertsToHex(uint value)
        {
            var str = CorrelationIdGenerator.GetNextId();
            Assert.Equal($"{str}:{value:X8}", StringUtilities.ConcatAsHexSuffix(str, ':', value));
        }

        [Fact]
        public void HandlesNull()
        {
            uint value = 0x23BC0234;
            Assert.Equal(":23BC0234", StringUtilities.ConcatAsHexSuffix(null, ':', value));
        }
    }
}
