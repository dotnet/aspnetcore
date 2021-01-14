// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Tasks;
using Microsoft.Build.Utilities;
using Xunit;

namespace Microsoft.NET.Sdk.Razor.Test
{
    public class ResolveAllScopedCssAssetsTest
    {
        [Fact]
        public void ResolveAllScopedCssAssets_IgnoresRegularCssFiles()
        {
            // Arrange
            var taskInstance = new ResolveAllScopedCssAssets()
            {
                StaticWebAssets = new[]
                {
                    new TaskItem("TestFiles/Pages/Counter.razor.rz.scp.css", new Dictionary<string,string>
                    {
                        ["RelativePath"] = "Pages/Counter.razor.rz.scp.css"
                    }),
                    new TaskItem("site.css", new Dictionary<string,string>
                    {
                        ["RelativePath"] = "site.css"
                    }),
                }
            };

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.True(result);
            var scopedCssAsset = Assert.Single(taskInstance.ScopedCssAssets);
            Assert.NotEqual("site.css", scopedCssAsset.ItemSpec);
        }

        [Fact]
        public void ResolveAllScopedCssAssets_DetectsScopedCssFiles()
        {
            // Arrange
            var taskInstance = new ResolveAllScopedCssAssets()
            {
                StaticWebAssets = new[]
                {
                    new TaskItem("TestFiles/Pages/Counter.razor.rz.scp.css", new Dictionary<string,string>
                    {
                        ["RelativePath"] = "Pages/Counter.razor.rz.scp.css"
                    }),
                    new TaskItem("site.css", new Dictionary<string,string>
                    {
                        ["RelativePath"] = "site.css"
                    }),
                }
            };

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.True(result);
            var scopedCssAsset = Assert.Single(taskInstance.ScopedCssAssets);
            Assert.Equal("TestFiles/Pages/Counter.razor.rz.scp.css", scopedCssAsset.ItemSpec);
        }

        [Fact]
        public void ResolveAllScopedCssAssets_DetectsScopedCssProjectBundleFiles()
        {
            // Arrange
            var taskInstance = new ResolveAllScopedCssAssets()
            {
                StaticWebAssets = new[]
                {
                    new TaskItem("Folder/Project.bundle.scp.css", new Dictionary<string,string>
                    {
                        ["RelativePath"] = "Project.bundle.scp.css"
                    }),
                    new TaskItem("site.css", new Dictionary<string,string>
                    {
                        ["RelativePath"] = "site.css"
                    }),
                }
            };

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.True(result);
            var scopedCssBundle = Assert.Single(taskInstance.ScopedCssProjectBundles);
            Assert.Equal("Folder/Project.bundle.scp.css", scopedCssBundle.ItemSpec);
        }

        [Fact]
        public void ResolveAllScopedCssAssets_IgnoresScopedCssApplicationBundleFiles()
        {
            // Arrange
            var taskInstance = new ResolveAllScopedCssAssets()
            {
                StaticWebAssets = new[]
                {
                    new TaskItem("Folder/Project.styles.css", new Dictionary<string,string>
                    {
                        ["RelativePath"] = "Project.styles.css"
                    }),
                    new TaskItem("site.css", new Dictionary<string,string>
                    {
                        ["RelativePath"] = "site.css"
                    }),
                }
            };

            // Act
            var result = taskInstance.Execute();

            // Assert
            Assert.True(result);
            Assert.Empty(taskInstance.ScopedCssProjectBundles);
        }
    }
}
