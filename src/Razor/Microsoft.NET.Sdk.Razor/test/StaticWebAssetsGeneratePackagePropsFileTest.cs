// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Razor.Tasks;
using Microsoft.Build.Framework;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class StaticWebAssetsGeneratePackagePropsFileTest
    {
        [Fact]
        public void WritesPropsFile_WithProvidedImportPath()
        {
            // Arrange
            var file = Path.GetTempFileName();
            var expectedDocument = @"<Project>
  <Import Project=""Microsoft.AspNetCore.StaticWebAssets.props"" />
</Project>";

            try
            {
                var buildEngine = new Mock<IBuildEngine>();

                var task = new StaticWebAssetsGeneratePackagePropsFile
                {
                    BuildEngine = buildEngine.Object,
                    PropsFileImport="Microsoft.AspNetCore.StaticWebAssets.props",
                    BuildTargetPath=file
                };

                // Act
                var result = task.Execute();

                // Assert
                Assert.True(result);
                var document = File.ReadAllText(file);
                Assert.Equal(expectedDocument, document, ignoreLineEndingDifferences: true);
            }
            finally
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }
    }
}
