// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    public class BootJsonWriterTest
    {
        [Fact]
        public void ProducesJsonReferencingAssemblyAndDependencies()
        {
            // Arrange/Act
            var assemblyReferences = new string[] { "MyApp.EntryPoint.dll", "System.Abc.dll", "MyApp.ClassLib.dll", };
            var content = BootJsonWriter.GetBootJsonContent(
                "MyApp.Entrypoint.dll",
                assemblyReferences,
                linkerEnabled: true);

            // Assert
            var parsedContent = JsonConvert.DeserializeObject<JObject>(content);
            Assert.Equal("MyApp.Entrypoint.dll", parsedContent["entryAssembly"].Value<string>());
            Assert.Equal(assemblyReferences, parsedContent["assemblies"].Values<string>());
        }
    }
}
