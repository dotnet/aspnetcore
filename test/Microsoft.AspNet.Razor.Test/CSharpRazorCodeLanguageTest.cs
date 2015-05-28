// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.AspNet.Razor.Parser;
using Xunit;

namespace Microsoft.AspNet.Razor.Test
{
    public class CSharpRazorCodeLanguageTest
    {
        [Fact]
        public void CreateCodeParserReturnsNewCSharpCodeParser()
        {
            // Arrange
            var service = new CSharpRazorCodeLanguage();

            // Act
            var parser = service.CreateCodeParser();

            // Assert
            Assert.NotNull(parser);
            Assert.IsType<CSharpCodeParser>(parser);
        }

        [Fact]
        public void CreateChunkGeneratorParserListenerReturnsNewCSharpChunkGeneratorParserListener()
        {
            // Arrange
            var service = new CSharpRazorCodeLanguage();

            // Act
            var host = new RazorEngineHost(service);
            var generator = service.CreateChunkGenerator("Foo", "Bar", "Baz", host);

            // Assert
            Assert.NotNull(generator);
            Assert.IsType<RazorChunkGenerator>(generator);
            Assert.Equal("Foo", generator.ClassName);
            Assert.Equal("Bar", generator.RootNamespaceName);
            Assert.Equal("Baz", generator.SourceFileName);
            Assert.Same(host, generator.Host);
        }

        [Fact]
        public void CreateCodeGenerator_ReturnsNewCSharpCodeGenerator()
        {
            // Arrange
            var language = new CSharpRazorCodeLanguage();
            var host = new RazorEngineHost(language);
            var codeGeneratorContext = new CodeGeneratorContext(
                host,
                "myclass",
                "myns",
                string.Empty,
                shouldGenerateLinePragmas: false,
                errorSink: new ErrorSink());

            // Act
            var generator = language.CreateCodeGenerator(codeGeneratorContext);

            // Assert
            Assert.IsType<CSharpCodeGenerator>(generator);
        }
    }
}
