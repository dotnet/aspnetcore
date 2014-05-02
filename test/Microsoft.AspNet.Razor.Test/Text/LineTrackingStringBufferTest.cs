// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using Microsoft.AspNet.Razor.Text;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Text
{
    public class LineTrackingStringBufferTest
    {
        [Fact]
        public void CtorInitializesProperties()
        {
            LineTrackingStringBuffer buffer = new LineTrackingStringBuffer();
            Assert.Equal(0, buffer.Length);
        }

        [Fact]
        public void CharAtCorrectlyReturnsLocation()
        {
            LineTrackingStringBuffer buffer = new LineTrackingStringBuffer();
            buffer.Append("foo\rbar\nbaz\r\nbiz");
            LineTrackingStringBuffer.CharacterReference chr = buffer.CharAt(14);
            Assert.Equal('i', chr.Character);
            Assert.Equal(new SourceLocation(14, 3, 1), chr.Location);
        }
    }
}
