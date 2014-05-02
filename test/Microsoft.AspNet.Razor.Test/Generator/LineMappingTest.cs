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

using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Text;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class LineMappingTest
    {
        [Fact]
        public void GeneratedCodeMappingsAreEqualIfDataIsEqual()
        {
            // Arrange
            var left = new LineMapping(
                new MappingLocation(new SourceLocation(1, 2, 3), 4),
                new MappingLocation(new SourceLocation(5, 6, 7), 8)
            );
            var right = new LineMapping(
                new MappingLocation(new SourceLocation(1, 2, 3), 4),
                new MappingLocation(new SourceLocation(5, 6, 7), 8)
            );

            // Assert
            Assert.True(left == right);
            Assert.True(left.Equals(right));
            Assert.True(right.Equals(left));
            Assert.True(Equals(left, right));
        }

        [Fact]
        public void GeneratedCodeMappingsAreNotEqualIfCodeLengthIsNotEqual()
        {
            // Arrange
            var left = new LineMapping(
                new MappingLocation(new SourceLocation(1, 2, 3), 4),
                new MappingLocation(new SourceLocation(5, 6, 7), 8)
            );
            var right = new LineMapping(
                new MappingLocation(new SourceLocation(1, 2, 3), 5),
                new MappingLocation(new SourceLocation(5, 6, 7), 9)
            );

            // Assert
            AssertNotEqual(left, right);
        }

        [Fact]
        public void GeneratedCodeMappingsAreNotEqualIfStartGeneratedColumnIsNotEqual()
        {
            // Arrange
            var left = new LineMapping(
                new MappingLocation(new SourceLocation(1, 2, 3), 4),
                new MappingLocation(new SourceLocation(5, 6, 7), 8)
            );
            var right = new LineMapping(
                new MappingLocation(new SourceLocation(1, 2, 3), 4),
                new MappingLocation(new SourceLocation(5, 6, 8), 8)
            );

            // Assert
            AssertNotEqual(left, right);
        }

        [Fact]
        public void GeneratedCodeMappingsAreNotEqualIfStartColumnIsNotEqual()
        {
            // Arrange
            var left = new LineMapping(
                new MappingLocation(new SourceLocation(1, 2, 3), 4),
                new MappingLocation(new SourceLocation(5, 6, 8), 8)
            );
            var right = new LineMapping(
                new MappingLocation(new SourceLocation(1, 2, 3), 4),
                new MappingLocation(new SourceLocation(5, 6, 7), 8)
            );

            // Assert
            AssertNotEqual(left, right);
        }

        [Fact]
        public void GeneratedCodeMappingsAreNotEqualIfStartLineIsNotEqual()
        {
            // Arrange
            var left = new LineMapping(
                new MappingLocation(new SourceLocation(1, 2, 3), 4),
                new MappingLocation(new SourceLocation(5, 5, 7), 8)
            );
            var right = new LineMapping(
                new MappingLocation(new SourceLocation(1, 1, 3), 4),
                new MappingLocation(new SourceLocation(5, 6, 7), 8)
            );

            // Assert
            AssertNotEqual(left, right);
        }

        [Fact]
        public void GeneratedCodeMappingsAreNotEqualIfAbsoluteIndexIsNotEqual()
        {
            // Arrange
            var left = new LineMapping(
                new MappingLocation(new SourceLocation(1, 2, 3), 4),
                new MappingLocation(new SourceLocation(4, 6, 7), 8)
            );
            var right = new LineMapping(
                new MappingLocation(new SourceLocation(1, 2, 3), 4),
                new MappingLocation(new SourceLocation(5, 6, 7), 9)
            );

            // Assert
            AssertNotEqual(left, right);
        }

        private void AssertNotEqual(LineMapping left, LineMapping right)
        {
            Assert.False(left == right);
            Assert.False(left.Equals(right));
            Assert.False(right.Equals(left));
            Assert.False(Equals(left, right));
        }
    }
}