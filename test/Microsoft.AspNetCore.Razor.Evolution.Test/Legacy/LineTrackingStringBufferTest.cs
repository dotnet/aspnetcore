// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    public class LineTrackingStringBufferTest
    {
        [Fact]
        public void CtorInitializesProperties()
        {
            var buffer = new LineTrackingStringBuffer(string.Empty);
            Assert.Equal(0, buffer.Length);
        }

        [Fact]
        public void CharAtCorrectlyReturnsLocation()
        {
            var buffer = new LineTrackingStringBuffer("foo\rbar\nbaz\r\nbiz");
            LineTrackingStringBuffer.CharacterReference chr = buffer.CharAt(14);
            Assert.Equal('i', chr.Character);
            Assert.Equal(new SourceLocation(14, 3, 1), chr.Location);
        }
    }
}
