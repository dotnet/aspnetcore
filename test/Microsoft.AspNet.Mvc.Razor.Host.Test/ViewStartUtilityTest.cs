// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.FileSystems;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;
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
            // Act
            var result = ViewStartUtility.GetViewStartLocations(new TestFileSystem(), viewPath);

            // Assert
            Assert.Empty(result);
        }

        [Theory]
        [InlineData("/views/Home/MyView.cshtml")]
        [InlineData("~/views/Home/MyView.cshtml")]
        [InlineData("views/Home/MyView.cshtml")]
        public void GetViewStartLocations_ReturnsPotentialViewStartLocations(string inputPath)
        {
            // Arrange
            var expected = new[]
            {
                @"views\Home\_viewstart.cshtml",
                @"views\_viewstart.cshtml",
                @"_viewstart.cshtml"
            };
            var fileSystem = new PhysicalFileSystem(GetTestFileSystemBase());

            // Act
            var result = ViewStartUtility.GetViewStartLocations(fileSystem, inputPath);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("/views/Home/_ViewStart.cshtml")]
        [InlineData("~/views/Home/_viewstart.cshtml")]
        [InlineData("views/Home/_Viewstart.cshtml")]
        public void GetViewStartLocations_SkipsCurrentPath_IfCurrentIsViewStart(string inputPath)
        {
            // Arrange
            var expected = new[]
            {
                @"views\_viewstart.cshtml",
                @"_viewstart.cshtml"
            };
            var fileSystem = new PhysicalFileSystem(GetTestFileSystemBase());

            // Act
            var result = ViewStartUtility.GetViewStartLocations(fileSystem, inputPath);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("Test.cshtml")]
        [InlineData("ViewStart.cshtml")]
        public void GetViewStartLocations_ReturnsPotentialViewStartLocations_IfPathIsAbsolute(string fileName)
        {
            // Arrange
            var expected = new[]
            {
                @"Areas\MyArea\Sub\Views\Admin\_viewstart.cshtml",
                @"Areas\MyArea\Sub\Views\_viewstart.cshtml",
                @"Areas\MyArea\Sub\_viewstart.cshtml",
                @"Areas\MyArea\_viewstart.cshtml",
                @"Areas\_viewstart.cshtml",
                @"_viewstart.cshtml",
            };
            var appBase = GetTestFileSystemBase();
            var viewPath = Path.Combine(appBase, "Areas", "MyArea", "Sub", "Views", "Admin", fileName);
            var fileSystem = new PhysicalFileSystem(appBase);

            // Act
            var result = ViewStartUtility.GetViewStartLocations(fileSystem, viewPath);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("_ViewStart.cshtml")]
        [InlineData("_viewstart.cshtml")]
        public void GetViewStartLocations_SkipsCurrentPath_IfAbsolutePathIsAViewStartFile(string fileName)
        {
            // Arrange
            var expected = new[]
            {
                @"Areas\MyArea\Sub\Views\_viewstart.cshtml",
                @"Areas\MyArea\Sub\_viewstart.cshtml",
                @"Areas\MyArea\_viewstart.cshtml",
                @"Areas\_viewstart.cshtml",
                @"_viewstart.cshtml",
            };
            var appBase = GetTestFileSystemBase();
            var viewPath = Path.Combine(appBase, "Areas", "MyArea", "Sub", "Views", "Admin", fileName);
            var fileSystem = new PhysicalFileSystem(appBase);

            // Act
            var result = ViewStartUtility.GetViewStartLocations(fileSystem, viewPath);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetViewStartLocations_ReturnsEmptySequence_IfViewStartIsAtRoot()
        {
            // Arrange
            var appBase = GetTestFileSystemBase();
            var viewPath = "_viewstart.cshtml";
            var fileSystem = new PhysicalFileSystem(appBase);

            // Act
            var result = ViewStartUtility.GetViewStartLocations(fileSystem, viewPath);

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