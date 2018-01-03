// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageSelectorModelTest
    {
        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR, SkipReason = "Fails due to dotnet/standard#567")]
        [InlineData("/Areas/About.cshtml")]
        [InlineData("/Areas/MyArea/Index.cshtml")]
        public void TryParseAreaPath_ReturnsFalse_IfPathDoesNotConform(string path)
        {
            // Arrange
            var options = new RazorPagesOptions();

            // Act
            var success = PageSelectorModel.TryParseAreaPath(options, path, NullLogger.Instance, out _);

            // Assert
            Assert.False(success);
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR, SkipReason = "Fails due to dotnet/standard#567")]
        [InlineData("/MyArea/Views/About.cshtml")]
        [InlineData("/MyArea/SubDir/Pages/Index.cshtml")]
        [InlineData("/MyArea/NotPages/SubDir/About.cshtml")]
        public void TryParseAreaPath_ReturnsFalse_IfPathDoesNotBelongToRootDirectory(string path)
        {
            // Arrange
            var options = new RazorPagesOptions();

            // Act
            var success = PageSelectorModel.TryParseAreaPath(options, path, NullLogger.Instance, out _);

            // Assert
            Assert.False(success);
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR, SkipReason = "Fails due to dotnet/standard#567")]
        [InlineData("/MyArea/Pages/Index.cshtml", "MyArea", "/Index", "/MyArea/Index")]
        [InlineData("/Accounts/Pages/Manage/Edit.cshtml", "Accounts", "/Manage/Edit", "/Accounts/Manage/Edit")]
        public void TryParseAreaPath_ParsesAreaPath(
            string path,
            string expectedArea,
            string expectedViewEnginePath,
            string expectedRoute)
        {
            // Arrange
            var options = new RazorPagesOptions();

            // Act
            var success = PageSelectorModel.TryParseAreaPath(options, path, NullLogger.Instance, out var result);

            // Assert
            Assert.True(success);
            Assert.Equal(expectedArea, result.areaName);
            Assert.Equal(expectedViewEnginePath, result.viewEnginePath);
            Assert.Equal(expectedRoute, result.pageRoute);
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR, SkipReason = "Fails due to dotnet/standard#567")]
        [InlineData("/MyArea/Dir1/Dir2/Index.cshtml", "MyArea", "/Index", "/MyArea/Index")]
        [InlineData("/Accounts/Dir1/Dir2/Manage/Edit.cshtml", "Accounts", "/Manage/Edit", "/Accounts/Manage/Edit")]
        public void TryParseAreaPath_ParsesAreaPath_WithMultiLevelRootDirectory(
            string path,
            string expectedArea,
            string expectedViewEnginePath,
            string expectedRoute)
        {
            // Arrange
            var options = new RazorPagesOptions
            {
                RootDirectory = "/Dir1/Dir2"
            };

            // Act
            var success = PageSelectorModel.TryParseAreaPath(options, path, NullLogger.Instance, out var result);

            // Assert
            Assert.True(success);
            Assert.Equal(expectedArea, result.areaName);
            Assert.Equal(expectedViewEnginePath, result.viewEnginePath);
            Assert.Equal(expectedRoute, result.pageRoute);
        }
    }
}
