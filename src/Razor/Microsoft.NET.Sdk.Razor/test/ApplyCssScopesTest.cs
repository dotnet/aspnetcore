// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Moq;
using Xunit;

namespace Microsoft.NET.Sdk.Razor.Test
{
    public class ApplyAllCssScopesTest
    {
        [Fact]
        public void ApplyAllCssScopes_AppliesScopesToRazorFiles()
        {
            // Arrange
            var taskInstance = new ApplyCssScopes()
            {
                RazorComponents = new[]
                {
                    new TaskItem("TestFiles/Pages/Counter.razor"),
                    new TaskItem("TestFiles/Pages/Index.razor"),
                },
                ScopedCss = new[]
                {
                    new TaskItem("TestFiles/Pages/Index.razor.css", new Dictionary<string, string> { ["CssScope"] = "index-scope" }),
                    new TaskItem("TestFiles/Pages/Counter.razor.css", new Dictionary<string, string> { ["CssScope"] = "counter-scope" }),
                }
            };

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.True(result);
            Assert.Equal(2, taskInstance.RazorComponentsWithScopes.Length);
            Assert.Single(taskInstance.RazorComponentsWithScopes, rcws => rcws.ItemSpec == "TestFiles/Pages/Index.razor" && rcws.GetMetadata("CssScope") == "index-scope");
            Assert.Single(taskInstance.RazorComponentsWithScopes, rcws => rcws.ItemSpec == "TestFiles/Pages/Counter.razor" && rcws.GetMetadata("CssScope") == "counter-scope");
        }

        [Fact]
        public void DoesNotApplyCssScopes_ToRazorComponentsWithoutAssociatedFiles()
        {
            // Arrange
            var taskInstance = new ApplyCssScopes()
            {
                RazorComponents = new[]
                {
                    new TaskItem("TestFiles/Pages/Counter.razor"),
                    new TaskItem("TestFiles/Pages/Index.razor"),
                    new TaskItem("TestFiles/Pages/FetchData.razor"),
                },
                ScopedCss = new[]
                {
                    new TaskItem("TestFiles/Pages/Index.razor.css", new Dictionary<string, string> { ["CssScope"] = "index-scope" }),
                    new TaskItem("TestFiles/Pages/Counter.razor.css", new Dictionary<string, string> { ["CssScope"] = "counter-scope" })
                }
            };

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.True(result);
            Assert.DoesNotContain(taskInstance.RazorComponentsWithScopes, rcws => rcws.ItemSpec == "TestFiles/Pages/Fetchdata.razor");
        }

        [Fact]
        public void ApplyAllCssScopes_FailsWhenTheScopedCss_DoesNotMatchTheRazorComponent()
        {
            // Arrange
            var taskInstance = new ApplyCssScopes()
            {
                RazorComponents = new[]
                {
                    new TaskItem("TestFiles/Pages/Counter.razor"),
                    new TaskItem("TestFiles/Pages/Index.razor"),
                },
                ScopedCss = new[]
                {
                    new TaskItem("TestFiles/Pages/Index.razor.css", new Dictionary<string, string> { ["CssScope"] = "index-scope" }),
                    new TaskItem("TestFiles/Pages/Counter.razor.css", new Dictionary<string, string> { ["CssScope"] = "counter-scope" }),
                    new TaskItem("TestFiles/Pages/Profile.razor.css", new Dictionary<string, string> { ["CssScope"] = "profile-scope" }),
                }
            };

            taskInstance.BuildEngine = Mock.Of<IBuildEngine>();

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ScopedCssCanDefineAssociatedRazorComponentFile()
        {
            // Arrange
            var taskInstance = new ApplyCssScopes()
            {
                RazorComponents = new[]
                {
                    new TaskItem("TestFiles/Pages/FetchData.razor")
                },
                ScopedCss = new[]
                {
                    new TaskItem("TestFiles/Pages/Profile.razor.css", new Dictionary<string, string>
                    {
                        ["CssScope"] = "fetchdata-scope",
                        ["RazorComponent"] = "TestFiles/Pages/FetchData.razor"
                    })
                }
            };

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.True(result);
            var rcws = Assert.Single(taskInstance.RazorComponentsWithScopes);
            Assert.Equal("TestFiles/Pages/FetchData.razor", rcws.ItemSpec);
            Assert.Equal("fetchdata-scope", rcws.GetMetadata("CssScope"));
        }

        [Fact]
        public void ApplyAllCssScopes_FailsWhenMultipleScopedCssFiles_MatchTheSameRazorComponent()
        {
            // Arrange
            var taskInstance = new ApplyCssScopes()
            {
                RazorComponents = new[]
                {
                    new TaskItem("TestFiles/Pages/Counter.razor"),
                    new TaskItem("TestFiles/Pages/Index.razor"),
                },
                ScopedCss = new[]
                {
                    new TaskItem("TestFiles/Pages/Index.razor.css", new Dictionary<string, string> { ["CssScope"] = "index-scope" }),
                    new TaskItem("TestFiles/Pages/Counter.razor.css", new Dictionary<string, string> { ["CssScope"] = "counter-scope" }),
                    new TaskItem("TestFiles/Pages/Profile.razor.css", new Dictionary<string, string>
                    {
                        ["CssScope"] = "conflict-scope",
                        ["RazorComponent"] = "TestFiles/Pages/Index.razor"
                    }),
                }
            };

            taskInstance.BuildEngine = Mock.Of<IBuildEngine>();

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.False(result);
        }
    }
}
