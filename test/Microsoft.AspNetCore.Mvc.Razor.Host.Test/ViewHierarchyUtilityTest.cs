// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class ViewHierarchyUtilityTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GetViewStartLocations_ReturnsEmptySequenceIfViewPathIsEmpty(string viewPath)
        {
            // Act
            var result = ViewHierarchyUtility.GetViewStartLocations(viewPath);

            // Assert
            Assert.Empty(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GetViewImportsLocations_ReturnsEmptySequenceIfViewPathIsEmpty(string viewPath)
        {
            // Act
            var result = ViewHierarchyUtility.GetViewImportsLocations(viewPath);

            // Assert
            Assert.Empty(result);
        }

        [Theory]
        [InlineData("/Views/Home/MyView.cshtml")]
        [InlineData("~/Views/Home/MyView.cshtml")]
        [InlineData("Views/Home/MyView.cshtml")]
        public void GetViewStartLocations_ReturnsPotentialViewStartLocations_PathStartswithSlash(string inputPath)
        {
            // Arrange
            var expected = new[]
            {
                "/Views/Home/_ViewStart.cshtml",
                "/Views/_ViewStart.cshtml",
                "/_ViewStart.cshtml"
            };

            // Act
            var result = ViewHierarchyUtility.GetViewStartLocations(inputPath);

            // Assert
            Assert.Equal(expected, result);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux,
            SkipReason = "Back slashes only work as path separators on Windows")]
        [OSSkipCondition(OperatingSystems.MacOSX,
            SkipReason = "Back slashes only work as path separators on Windows")]
        [InlineData(@"~/Views\Home\MyView.cshtml")]
        [InlineData(@"Views\Home\MyView.cshtml")]
        public void GetViewStartLocations_ReturnsPotentialViewStartLocations_PathsContainBackSlash(
            string inputPath)
        {
            // Arrange
            var expected = new[]
            {
                "/Views/Home/_ViewStart.cshtml",
                "/Views/_ViewStart.cshtml",
                "/_ViewStart.cshtml"
            };

            // Act
            var result = ViewHierarchyUtility.GetViewStartLocations(inputPath);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("/Views/Home/MyView.cshtml")]
        [InlineData("~/Views/Home/MyView.cshtml")]
        [InlineData("Views/Home/MyView.cshtml")]
        public void GetViewImportsLocations_ReturnsPotentialViewStartLocations_PathStartswithSlash(string inputPath)
        {
            // Arrange
            var expected = new[]
            {
                "/Views/Home/_ViewImports.cshtml",
                "/Views/_ViewImports.cshtml",
                "/_ViewImports.cshtml"
            };

            // Act
            var result = ViewHierarchyUtility.GetViewImportsLocations(inputPath);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("/Views/Home/_ViewStart.cshtml")]
        [InlineData("~/Views/Home/_ViewStart.cshtml")]
        [InlineData("Views/Home/_ViewStart.cshtml")]
        public void GetViewStartLocations_SkipsCurrentPath_IfCurrentIsViewStart(string inputPath)
        {
            // Arrange
            var expected = new[]
            {
                "/Views/_ViewStart.cshtml",
                "/_ViewStart.cshtml"
            };

            // Act
            var result = ViewHierarchyUtility.GetViewStartLocations(inputPath);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("/Views/Home/_ViewStart.cshtml")]
        [InlineData("~/Views/Home/_ViewStart.cshtml")]
        [InlineData("Views/Home/_ViewStart.cshtml")]
        public void GetViewImportsLocations_WhenCurrentIsViewStart(string inputPath)
        {
            // Arrange
            var expected = new[]
            {
                "/Views/Home/_ViewImports.cshtml",
                "/Views/_ViewImports.cshtml",
                "/_ViewImports.cshtml"
            };

            // Act
            var result = ViewHierarchyUtility.GetViewImportsLocations(inputPath);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("/Views/Home/_ViewImports.cshtml")]
        [InlineData("~/Views/Home/_ViewImports.cshtml")]
        [InlineData("Views/Home/_ViewImports.cshtml")]
        public void GetViewImportsLocations_SkipsCurrentPath_IfCurrentIsViewImports(string inputPath)
        {
            // Arrange
            var expected = new[]
            {
                "/Views/_ViewImports.cshtml",
                "/_ViewImports.cshtml"
            };

            // Act
            var result = ViewHierarchyUtility.GetViewImportsLocations(inputPath);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("Test.cshtml")]
        [InlineData("ViewStart.cshtml")]
        public void GetViewStartLocations_ReturnsPotentialViewStartLocations(string fileName)
        {
            // Arrange
            var expected = new[]
            {
                "/Areas/MyArea/Sub/Views/Admin/_ViewStart.cshtml",
                "/Areas/MyArea/Sub/Views/_ViewStart.cshtml",
                "/Areas/MyArea/Sub/_ViewStart.cshtml",
                "/Areas/MyArea/_ViewStart.cshtml",
                "/Areas/_ViewStart.cshtml",
                "/_ViewStart.cshtml",
            };
            var viewPath = $"Areas/MyArea/Sub/Views/Admin/{fileName}";

            // Act
            var result = ViewHierarchyUtility.GetViewStartLocations(viewPath);

            // Assert
            Assert.Equal(expected, result);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux,
            SkipReason = "Back slashes only work as path separators on Windows")]
        [OSSkipCondition(OperatingSystems.MacOSX,
            SkipReason = "Back slashes only work as path separators on Windows")]
        [InlineData("Test.cshtml")]
        [InlineData("ViewStart.cshtml")]
        public void GetViewStartLocations_ReturnsPotentialViewStartLocations_ForPathsWithBackSlashes(string fileName)
        {
            // Arrange
            var expected = new[]
            {
                "/Areas/MyArea/Sub/Views/Admin/_ViewStart.cshtml",
                "/Areas/MyArea/Sub/Views/_ViewStart.cshtml",
                "/Areas/MyArea/Sub/_ViewStart.cshtml",
                "/Areas/MyArea/_ViewStart.cshtml",
                "/Areas/_ViewStart.cshtml",
                "/_ViewStart.cshtml",
            };
            var viewPath = $"Areas\\MyArea\\Sub\\Views\\Admin/{fileName}";

            // Act
            var result = ViewHierarchyUtility.GetViewStartLocations(viewPath);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("Test.cshtml")]
        [InlineData("Global.cshtml")]
        [InlineData("_ViewStart.cshtml")]
        public void GetViewImportsLocations_ReturnsPotentialGlobalLocations(string fileName)
        {
            // Arrange
            var expected = new[]
            {
                "/Areas/MyArea/Sub/Views/Admin/_ViewImports.cshtml",
                "/Areas/MyArea/Sub/Views/_ViewImports.cshtml",
                "/Areas/MyArea/Sub/_ViewImports.cshtml",
                "/Areas/MyArea/_ViewImports.cshtml",
                "/Areas/_ViewImports.cshtml",
                "/_ViewImports.cshtml",
            };
            var viewPath = $"Areas/MyArea/Sub/Views/Admin/{fileName}";

            // Act
            var result = ViewHierarchyUtility.GetViewImportsLocations(viewPath);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("_ViewStart.cshtml")]
        [InlineData("_viewstart.cshtml")]
        public void GetViewStartLocations_SkipsCurrentPath_IfPathIsAViewStartFile(string fileName)
        {
            // Arrange
            var expected = new[]
            {
                "/Areas/MyArea/Sub/Views/_ViewStart.cshtml",
                "/Areas/MyArea/Sub/_ViewStart.cshtml",
                "/Areas/MyArea/_ViewStart.cshtml",
                "/Areas/_ViewStart.cshtml",
                "/_ViewStart.cshtml",
            };
            var viewPath = $"Areas/MyArea/Sub/Views/Admin/{fileName}";

            // Act
            var result = ViewHierarchyUtility.GetViewStartLocations(viewPath);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetViewStartLocations_ReturnsEmptySequence_IfViewStartIsAtRoot()
        {
            // Arrange
            var viewPath = "_ViewStart.cshtml";

            // Act
            var result = ViewHierarchyUtility.GetViewStartLocations(viewPath);

            // Assert
            Assert.Empty(result);
        }
    }
}