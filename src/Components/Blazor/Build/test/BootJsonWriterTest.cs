// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build
{
    public class BootJsonWriterTest
    {
        [Fact]
        public async Task ProducesJsonReferencingAssemblyAndDependencies()
        {
            // Arrange/Act
            var assemblyReferences = new string[] { "MyApp.EntryPoint.dll", "System.Abc.dll", "MyApp.ClassLib.dll", };
            using var stream = new MemoryStream();

            // Act
            GenerateBlazorBootJson.WriteBootJson(
                stream,
                "MyApp.Entrypoint.dll",
                assemblyReferences,
                linkerEnabled: true);

            // Assert
            stream.Position = 0;
            using var parsedContent = await JsonDocument.ParseAsync(stream);
            var rootElement = parsedContent.RootElement;
            Assert.Equal("MyApp.Entrypoint.dll", rootElement.GetProperty("entryAssembly").GetString());
            var assembliesElement = rootElement.GetProperty("assemblies");
            Assert.Equal(assemblyReferences.Length, assembliesElement.GetArrayLength());
            for (var i = 0; i < assemblyReferences.Length; i++)
            {
                Assert.Equal(assemblyReferences[i], assembliesElement[i].GetString());
            }
            Assert.True(rootElement.GetProperty("linkerEnabled").GetBoolean());
        }
    }
}
