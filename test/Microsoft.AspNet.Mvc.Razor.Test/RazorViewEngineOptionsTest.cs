// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorViewEngineOptionsTest
    {
        [Fact]
        public void FileSystemThrows_IfNullIsAsseigned()
        {
            // Arrange
            var options = new RazorViewEngineOptions();

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => options.FileSystem = null);
            Assert.Equal("value", ex.ParamName);
        }
    }
}