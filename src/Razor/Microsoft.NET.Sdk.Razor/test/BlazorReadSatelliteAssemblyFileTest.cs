// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class BlazorReadSatelliteAssemblyFileTest
    {
        [Fact]
        public void WritesAndReadsRoundTrip()
        {
            // Arrange/Act
            var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            var writer = new BlazorWriteSatelliteAssemblyFile
            {
                BuildEngine = Mock.Of<IBuildEngine>(),
                WriteFile = new TaskItem(tempFile),
                SatelliteAssembly = new[]
                {
                    new TaskItem("Resources.fr.dll", new Dictionary<string, string>
                    {
                        ["Culture"] = "fr",
                        ["DestinationSubDirectory"] = "fr\\",
                    }),
                    new TaskItem("Resources.ja-jp.dll", new Dictionary<string, string>
                    {
                        ["Culture"] = "ja-jp",
                        ["DestinationSubDirectory"] = "ja-jp\\",
                    }),
                },
            };

            var reader = new BlazorReadSatelliteAssemblyFile
            {
                BuildEngine = Mock.Of<IBuildEngine>(),
                ReadFile = new TaskItem(tempFile),
            };

            writer.Execute();

            Assert.True(File.Exists(tempFile), "Write should have succeeded.");

            reader.Execute();

            Assert.Collection(
                reader.SatelliteAssembly,
                assembly =>
                {
                    Assert.Equal("Resources.fr.dll", assembly.ItemSpec);
                    Assert.Equal("fr", assembly.GetMetadata("Culture"));
                    Assert.Equal("fr\\", assembly.GetMetadata("DestinationSubDirectory"));
                },
                assembly =>
                {
                    Assert.Equal("Resources.ja-jp.dll", assembly.ItemSpec);
                    Assert.Equal("ja-jp", assembly.GetMetadata("Culture"));
                    Assert.Equal("ja-jp\\", assembly.GetMetadata("DestinationSubDirectory"));
                });
        }
    }
}
