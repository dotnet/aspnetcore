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

using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Http
{
    public class PathStringTests
    {
        [Fact]
        public void CtorThrows_IfPathDoesNotHaveLeadingSlash()
        {
            // Act and Assert
            ExceptionAssert.ThrowsArgument(() => new PathString("hello"), "value", "");
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        public void AddPathString_HandlesNullAndEmptyStrings(string appString, string concatString)
        {
            // Arrange
            var appPath = new PathString(appString);
            var concatPath = new PathString(concatString);

            // Act
            var result = appPath.Add(concatPath);

            // Assert
            Assert.False(result.HasValue);
        }

        [Theory]
        [InlineData("", "/", "/")]
        [InlineData("/", null, "/")]
        [InlineData("/", "", "/")]
        [InlineData("/", "/test", "/test")]
        [InlineData("/myapp/", "/test/bar", "/myapp/test/bar")]
        [InlineData("/myapp/", "/test/bar/", "/myapp/test/bar/")]
        public void AddPathString_HandlesLeadingAndTrailingSlashes(string appString, string concatString, string expected)
        {
            // Arrange
            var appPath = new PathString(appString);
            var concatPath = new PathString(concatString);

            // Act
            var result = appPath.Add(concatPath);

            // Assert
            Assert.Equal(expected, result.Value);
        }
    }
}
