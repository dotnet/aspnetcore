// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.FileSystems;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class ViewStartUtilityTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GetViewStartLocations_ReturnsEmptySequenceIfViewPathIsEmpty(string viewPath)
        {
            // Act
            var result = ViewStartUtility.GetViewStartLocations(viewPath);

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
                @"Views\Home\_ViewStart.cshtml",
                @"Views\_ViewStart.cshtml",
                @"_ViewStart.cshtml"
            };

            // Act
            var result = ViewStartUtility.GetViewStartLocations(inputPath);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("/Views/Home/_ViewStart.cshtml")]
        [InlineData("~/Views/Home/_Viewstart.cshtml")]
        [InlineData("Views/Home/_Viewstart.cshtml")]
        public void GetViewStartLocations_SkipsCurrentPath_IfCurrentIsViewStart(string inputPath)
        {
            // Arrange
            var expected = new[]
            {
                @"Views\_ViewStart.cshtml",
                @"_ViewStart.cshtml"
            };
            var fileSystem = new PhysicalFileSystem(GetTestFileSystemBase());

            // Act
            var result = ViewStartUtility.GetViewStartLocations(inputPath);

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
                @"Areas\MyArea\Sub\Views\Admin\_ViewStart.cshtml",
                @"Areas\MyArea\Sub\Views\_ViewStart.cshtml",
                @"Areas\MyArea\Sub\_ViewStart.cshtml",
                @"Areas\MyArea\_ViewStart.cshtml",
                @"Areas\_ViewStart.cshtml",
                @"_ViewStart.cshtml",
            };
            var viewPath = Path.Combine("Areas", "MyArea", "Sub", "Views", "Admin", fileName);

            // Act
            var result = ViewStartUtility.GetViewStartLocations(viewPath);

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
                @"Areas\MyArea\Sub\Views\_ViewStart.cshtml",
                @"Areas\MyArea\Sub\_ViewStart.cshtml",
                @"Areas\MyArea\_ViewStart.cshtml",
                @"Areas\_ViewStart.cshtml",
                @"_ViewStart.cshtml",
            };
            var viewPath = Path.Combine("Areas", "MyArea", "Sub", "Views", "Admin", fileName);

            // Act
            var result = ViewStartUtility.GetViewStartLocations(viewPath);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetViewStartLocations_ReturnsEmptySequence_IfViewStartIsAtRoot()
        {
            // Arrange
            var appBase = GetTestFileSystemBase();
            var viewPath = "_ViewStart.cshtml";

            // Act
            var result = ViewStartUtility.GetViewStartLocations(viewPath);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetViewStartLocations_ReturnsEmptySequence_IfPathIsRooted()
        {
            // Arrange
            var appBase = GetTestFileSystemBase();
            var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "Index.cshtml");

            // Act
            var result = ViewStartUtility.GetViewStartLocations(absolutePath);

            // Assert
            Assert.Empty(result);
        }

        private static string GetTestFileSystemBase()
        {
            var serviceProvider = CallContextServiceLocator.Locator.ServiceProvider;
            var appEnv = (IApplicationEnvironment)serviceProvider.GetService(typeof(IApplicationEnvironment));
            return Path.Combine(appEnv.ApplicationBasePath, "TestFiles", "ViewStartUtilityFiles");
        }
    }
}