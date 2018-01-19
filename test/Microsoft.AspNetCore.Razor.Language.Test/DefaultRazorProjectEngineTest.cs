// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultRazorProjectEngineTest
    {
        [Fact]
        public void Process_AsksFileSystemForItems()
        {
            // Arrange
            var razorProjectItem = new TestRazorProjectItem("/some/path.cshtml");
            var testFileSystem = new Mock<RazorProjectFileSystem>();
            testFileSystem.Setup(fileSystem => fileSystem.GetItem("/some/path.cshtml"))
                .Returns(razorProjectItem)
                .Verifiable();
            var projectEngine = RazorProjectEngine.Create(testFileSystem.Object);

            // Act
            projectEngine.Process("/some/path.cshtml");

            // Assert
            testFileSystem.Verify();
        }
    }
}
