// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class ViewPathTest
    {
        [Theory]
        [InlineData("/Views/Home/Index.cshtml")]
        [InlineData("\\Views/Home/Index.cshtml")]
        [InlineData("\\Views\\Home/Index.cshtml")]
        [InlineData("\\Views\\Home\\Index.cshtml")]
        public void NormalizePath_NormalizesSlashes(string input)
        {
            // Act
            var normalizedPath = ViewPath.NormalizePath(input);

            // Assert
            Assert.Equal("/Views/Home/Index.cshtml", normalizedPath);
        }

        [Theory]
        [InlineData("Views/Home/Index.cshtml")]
        [InlineData("Views\\Home\\Index.cshtml")]
        public void NormalizePath_AppendsLeadingSlash(string input)
        {
            // Act
            var normalizedPath = ViewPath.NormalizePath(input);

            // Assert
            Assert.Equal("/Views/Home/Index.cshtml", normalizedPath);
        }
    }
}
