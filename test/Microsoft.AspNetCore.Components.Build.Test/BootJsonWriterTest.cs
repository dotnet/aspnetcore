// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Components.Build.Test
{
    public class BootJsonWriterTest
    {
        [Fact]
        public void ProducesJsonReferencingAssemblyAndDependencies()
        {
            // Arrange/Act
            var assemblyReferences = new string[] { "System.Abc.dll", "MyApp.ClassLib.dll", };
            var content = BootJsonWriter.GetBootJsonContent(
                "MyApp.Entrypoint.dll",
                "MyNamespace.MyType::MyMethod",
                assemblyReferences,
                Enumerable.Empty<EmbeddedResourceInfo>(),
                linkerEnabled: true);

            // Assert
            var parsedContent = JsonConvert.DeserializeObject<JObject>(content);
            Assert.Equal("MyApp.Entrypoint.dll", parsedContent["main"].Value<string>());
            Assert.Equal("MyNamespace.MyType::MyMethod", parsedContent["entryPoint"].Value<string>());
            Assert.Equal(assemblyReferences, parsedContent["assemblyReferences"].Values<string>());
        }

        [Fact]
        public void IncludesReferencesToEmbeddedContent()
        {
            // Arrange/Act
            var embeddedContent = new[]
            {
                new EmbeddedResourceInfo(EmbeddedResourceKind.Static, "my/static/file"),
                new EmbeddedResourceInfo(EmbeddedResourceKind.Css, "css/first.css"),
                new EmbeddedResourceInfo(EmbeddedResourceKind.JavaScript, "javascript/first.js"),
                new EmbeddedResourceInfo(EmbeddedResourceKind.Css, "css/second.css"),
                new EmbeddedResourceInfo(EmbeddedResourceKind.JavaScript, "javascript/second.js"),
            };
            var content = BootJsonWriter.GetBootJsonContent(
                "MyApp.Entrypoint",
                "MyNamespace.MyType::MyMethod",
                assemblyReferences: new[] { "Something.dll" },
                embeddedContent: embeddedContent,
                linkerEnabled: true);

            // Assert
            var parsedContent = JsonConvert.DeserializeObject<JObject>(content);
            Assert.Equal(
                new[] { "css/first.css", "css/second.css" },
                parsedContent["cssReferences"].Values<string>());
            Assert.Equal(
                new[] { "javascript/first.js", "javascript/second.js" },
                parsedContent["jsReferences"].Values<string>());
        }
    }
}
