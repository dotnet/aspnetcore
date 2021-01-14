// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Build.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class DiscoverDefaultScopedCssItemsTests
    {
        [Fact]
        public void DiscoversScopedCssFiles_BasedOnTheirExtension()
        {
            // Arrange
            var taskInstance = new DiscoverDefaultScopedCssItems()
            {
                Content = new[]
                {
                    new TaskItem("TestFiles/Pages/Counter.razor.css"),
                    new TaskItem("TestFiles/Pages/Index.razor.css"),
                    new TaskItem("TestFiles/Pages/Profile.razor.css"),
                }
            };

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.True(result);
            Assert.Equal(3, taskInstance.DiscoveredScopedCssInputs.Length);
        }

        [Fact]
        public void DiscoversScopedCssFiles_SkipsFilesWithScopedAttributeWithAFalseValue()
        {
            // Arrange
            var taskInstance = new DiscoverDefaultScopedCssItems()
            {
                Content = new[]
                {
                    new TaskItem("TestFiles/Pages/Counter.razor.css"),
                    new TaskItem("TestFiles/Pages/Index.razor.css"),
                    new TaskItem("TestFiles/Pages/Profile.razor.css", new Dictionary<string,string>{ ["Scoped"] = "false" }),
                }
            };

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.True(result);
            Assert.Equal(2, taskInstance.DiscoveredScopedCssInputs.Length);
        }
    }
}
