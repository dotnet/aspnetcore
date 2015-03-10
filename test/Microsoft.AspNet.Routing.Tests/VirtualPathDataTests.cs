// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using Moq;
using Xunit;

namespace Microsoft.AspNet.Routing
{
    public class VirtualPathDataTests
    {
        [Fact]
        public void Constructor_CreatesEmptyDataTokensIfNull()
        {
            // Arrange
            var router = new Mock<IRouter>().Object;
            var path = "virtual path";

            // Act
            var pathData = new VirtualPathData(router, path, null);

            // Assert
            Assert.Same(router, pathData.Router);
            Assert.Same(path, pathData.VirtualPath);
            Assert.NotNull(pathData.DataTokens);
            Assert.Empty(pathData.DataTokens);
        }

        [Fact]
        public void Constructor_CopiesDataTokens()
        {
            // Arrange
            var router = new Mock<IRouter>().Object;
            var path = "virtual path";
            var dataTokens = new RouteValueDictionary();
            dataTokens["TestKey"] = "TestValue";

            // Act
            var pathData = new VirtualPathData(router, path, dataTokens);

            // Assert
            Assert.Same(router, pathData.Router);
            Assert.Same(path, pathData.VirtualPath);
            Assert.NotNull(pathData.DataTokens);
            Assert.Equal("TestValue", pathData.DataTokens["TestKey"]);
            Assert.Equal(1, pathData.DataTokens.Count);
            Assert.NotSame(dataTokens, pathData.DataTokens);
        }

        [Fact]
        public void VirtualPath_ReturnsEmptyStringIfNull()
        {
            // Arrange
            var router = new Mock<IRouter>().Object;

            // Act
            var pathData = new VirtualPathData(router, virtualPath: null);

            // Assert
            Assert.Same(router, pathData.Router);
            Assert.Equal(string.Empty, pathData.VirtualPath);
            Assert.NotNull(pathData.DataTokens);
            Assert.Empty(pathData.DataTokens);
        }
    }
}
#endif