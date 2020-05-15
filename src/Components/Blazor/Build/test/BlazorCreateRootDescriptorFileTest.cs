// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Xml.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build
{
    public class BlazorCreateRootDescriptorFileTest
    {
        [Fact]
        public void ProducesRootDescriptor()
        {
            // Arrange/Act
            using var stream = new MemoryStream();

            // Act
            BlazorCreateRootDescriptorFile.WriteRootDescriptor(
                stream,
                new[] { "MyApp.dll" });

            // Assert
            stream.Position = 0;
            var document = XDocument.Load(stream);
            var rootElement = document.Root;

            var assemblyElement = Assert.Single(rootElement.Elements());
            Assert.Equal("assembly", assemblyElement.Name.ToString());
            Assert.Equal("MyApp.dll", assemblyElement.Attribute("fullname").Value);

            var typeElement = Assert.Single(assemblyElement.Elements());
            Assert.Equal("type", typeElement.Name.ToString());
            Assert.Equal("*", typeElement.Attribute("fullname").Value);
            Assert.Equal("true", typeElement.Attribute("required").Value);
        }
    }
}
