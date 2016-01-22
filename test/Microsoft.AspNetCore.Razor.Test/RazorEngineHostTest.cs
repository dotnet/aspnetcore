// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.AspNet.Razor.Parser;
using Xunit;

namespace Microsoft.AspNet.Razor.Test
{
    public class RazorEngineHostTest
    {
        [Fact]
        public void ConstructorWithCodeLanguageSetsPropertiesAppropriately()
        {
            // Arrange
            var language = new CSharpRazorCodeLanguage();

            // Act
            var host = new RazorEngineHost(language);

            // Assert
            VerifyCommonDefaults(host);
            Assert.Same(language, host.CodeLanguage);
            Assert.IsType<HtmlMarkupParser>(host.CreateMarkupParser());
        }

        [Fact]
        public void ConstructorWithCodeLanguageAndMarkupParserSetsPropertiesAppropriately()
        {
            // Arrange
            var language = new CSharpRazorCodeLanguage();
            var expected = new HtmlMarkupParser();

            // Act
            var host = new RazorEngineHost(language, () => expected);

            // Assert
            VerifyCommonDefaults(host);
            Assert.Same(language, host.CodeLanguage);
            Assert.Same(expected, host.CreateMarkupParser());
        }

        [Fact]
        public void DecorateCodeParserDoesNotModifyIncomingParser()
        {
            // Arrange
            var expected = new CSharpCodeParser();

            // Act
            var actual = CreateHost().DecorateCodeParser(expected);

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void DecorateMarkupParserReturnsIncomingParser()
        {
            // Arrange
            var expected = new HtmlMarkupParser();

            // Act
            var actual = CreateHost().DecorateMarkupParser(expected);

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void DecorateChunkGeneratorReturnsIncomingChunkGenerator()
        {
            // Arrange
            var expected = new RazorChunkGenerator("Foo", "Bar", "Baz", CreateHost());

            // Act
            var actual = CreateHost().DecorateChunkGenerator(expected);

            // Assert
            Assert.Same(expected, actual);
        }

        private static RazorEngineHost CreateHost()
        {
            return new RazorEngineHost(new CSharpRazorCodeLanguage());
        }

        private static void VerifyCommonDefaults(RazorEngineHost host)
        {
            Assert.Equal(GeneratedClassContext.Default, host.GeneratedClassContext);
            Assert.Empty(host.NamespaceImports);
            Assert.False(host.DesignTimeMode);
            Assert.Equal(RazorEngineHost.InternalDefaultClassName, host.DefaultClassName);
            Assert.Equal(RazorEngineHost.InternalDefaultNamespace, host.DefaultNamespace);
        }
    }
}
