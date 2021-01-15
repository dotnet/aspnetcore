// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class ComputeCssScopesTests
    {
        [Fact]
        public void ComputesScopes_ComputesUniqueScopes_ForCssFiles()
        {
            // Arrange
            var taskInstance = new ComputeCssScope()
            {
                ScopedCssInput = new[]
                {
                    new TaskItem("TestFiles/Pages/Counter.razor.css"),
                    new TaskItem("TestFiles/Pages/Index.razor.css"),
                    new TaskItem("TestFiles/Pages/Profile.razor.css"),
                },
                TargetName = "Test"
            };

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.True(result);
            Assert.Equal(3, taskInstance.ScopedCss.Length);
            Assert.All(taskInstance.ScopedCss, item =>
            {
                var scope = item.GetMetadata("CssScope");
                Assert.NotEmpty(scope);
                Assert.Matches("b-[a-z0-9]+", scope);
            });

            Assert.Equal(3, new HashSet<string>(taskInstance.ScopedCss.Select(s => s.GetMetadata("CssScope"))).Count);
        }

        [Fact]
        public void ComputesScopes_ScopeVariesByTargetName()
        {
            // Arrange
            var taskInstance = new ComputeCssScope()
            {
                ScopedCssInput = new[]
                {
                    new TaskItem("TestFiles/Pages/Counter.razor.css"),
                    new TaskItem("TestFiles/Pages/Index.razor.css"),
                    new TaskItem("TestFiles/Pages/Profile.razor.css"),
                },
                TargetName = "Test"
            };

            // Act
            taskInstance.Execute();
            var existing = taskInstance.ScopedCss.Select(s => s.GetMetadata("CssScope")).ToArray();

            taskInstance.TargetName = "AnotherLibrary";
            var result = taskInstance.Execute();

            // Assert
            Assert.All(taskInstance.ScopedCss, newScoped => Assert.DoesNotContain(newScoped.GetMetadata("ScopedCss"), existing));
        }

        [Fact]
        public void ComputesScopes_IsDeterministic()
        {
            // Arrange
            var taskInstance = new ComputeCssScope()
            {
                ScopedCssInput = new[]
                {
                    new TaskItem("TestFiles/Pages/Counter.razor.css"),
                    new TaskItem("TestFiles/Pages/Index.razor.css"),
                    new TaskItem("TestFiles/Pages/Profile.razor.css"),
                },
                TargetName = "Test"
            };

            // Act
            taskInstance.Execute();
            var existing = taskInstance.ScopedCss.Select(s => s.GetMetadata("CssScope")).OrderBy(id => id).ToArray();

            var result = taskInstance.Execute();

            // Assert
            Assert.Equal(existing, taskInstance.ScopedCss.Select(newScoped => newScoped.GetMetadata("CssScope")).OrderBy(id => id).ToArray());
        }

        [Fact]
        public void ComputesScopes_VariesByPath()
        {
            // Arrange
            var taskInstance = new ComputeCssScope()
            {
                ScopedCssInput = new[]
                {
                    new TaskItem("TestFiles/Pages/Index.razor.css"),
                    new TaskItem("TestFiles/Index.razor.css"),
                },
                TargetName = "Test"
            };

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.True(result);
            Assert.Equal(2, taskInstance.ScopedCss.Length);
            Assert.NotEqual(taskInstance.ScopedCss[0].GetMetadata("CssScope"), taskInstance.ScopedCss[1].GetMetadata("CssScope"));
        }

        [Fact]
        public void ComputesScopes_PreservesUserDefinedScopes()
        {
            // Arrange
            var taskInstance = new ComputeCssScope()
            {
                ScopedCssInput = new[]
                {
                    new TaskItem("TestFiles/Pages/Index.razor.css", new Dictionary<string,string>{ ["CssScope"] = "b-predefined" }),                },
                TargetName = "Test"
            };

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.True(result);
            var scopedCss = Assert.Single(taskInstance.ScopedCss);
            Assert.Equal("b-predefined", scopedCss.GetMetadata("CssScope"));
        }
    }
}
