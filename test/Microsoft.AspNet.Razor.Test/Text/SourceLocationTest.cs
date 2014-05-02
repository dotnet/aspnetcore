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
    public class SourceLocationTest
    {
        [Fact]
        public void ConstructorWithLineAndCharacterIndexSetsAssociatedProperties()
        {
            // Act
            SourceLocation loc = new SourceLocation(0, 42, 24);

            // Assert
            Assert.Equal(0, loc.AbsoluteIndex);
            Assert.Equal(42, loc.LineIndex);
            Assert.Equal(24, loc.CharacterIndex);
        }
    }
}
