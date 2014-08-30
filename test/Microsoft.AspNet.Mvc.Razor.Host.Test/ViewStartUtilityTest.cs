// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class ViewStartProviderTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GetViewStartLocations_ReturnsEmptySequenceIfViewPathIsEmpty(string viewPath)
        {
            // Arrange
            var appPath = @"x:\test";

            // Act
            var result = ViewStartUtility.GetViewStartLocations(appPath, viewPath);

            // Assert
            Assert.Empty(result);
        }

        public static IEnumerable<object[]> GetViewStartLocations_ReturnsPotentialViewStartLocationsData
        {
            get
            {
                yield return new object[]
                {
                    @"x:\test\myapp",
                    "/Views/Home/View.cshtml",
                    new[]
                    {
                        @"x:\test\myapp\Views\Home\_viewstart.cshtml",
                        @"x:\test\myapp\Views\_viewstart.cshtml",
                        @"x:\test\myapp\_viewstart.cshtml",
                    }
                };

                yield return new object[]
                {
                    @"x:\test\myapp",
                    "Views/Home/View.cshtml",
                    new[]
                    {
                        @"x:\test\myapp\Views\Home\_viewstart.cshtml",
                        @"x:\test\myapp\Views\_viewstart.cshtml",
                        @"x:\test\myapp\_viewstart.cshtml",
                    }
                };

                yield return new object[]
                {
                    @"x:\test\myapp\",
                    "Views/Home/View.cshtml",
                    new[]
                    {
                        @"x:\test\myapp\Views\Home\_viewstart.cshtml",
                        @"x:\test\myapp\Views\_viewstart.cshtml",
                        @"x:\test\myapp\_viewstart.cshtml",
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(GetViewStartLocations_ReturnsPotentialViewStartLocationsData))]
        public void GetViewStartLocations_ReturnsPotentialViewStartLocations(string appPath,
                                                                             string viewPath,
                                                                             IEnumerable<string> expected)
        {
            // Act
            var result = ViewStartUtility.GetViewStartLocations(appPath, viewPath);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}