// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test
{
    public class RazorEngineHostTest
    {
        [Fact]
        public void ConstructorRequiresNonNullCodeLanguage()
        {
            Assert.ThrowsArgumentNull(() => new RazorEngineHost(null), "codeLanguage");
            Assert.ThrowsArgumentNull(() => new RazorEngineHost(null, () => new HtmlMarkupParser()), "codeLanguage");
        }

        [Fact]
        public void ConstructorRequiresNonNullMarkupParser()
        {
            Assert.ThrowsArgumentNull(() => new RazorEngineHost(new CSharpRazorCodeLanguage(), null), "markupParserFactory");
        }

        [Fact]
        public void ConstructorWithCodeLanguageSetsPropertiesAppropriately()
        {
            // Arrange
            RazorCodeLanguage language = new CSharpRazorCodeLanguage();

            // Act
            RazorEngineHost host = new RazorEngineHost(language);

            // Assert
            VerifyCommonDefaults(host);
            Assert.Same(language, host.CodeLanguage);
            Assert.IsType<HtmlMarkupParser>(host.CreateMarkupParser());
        }

        [Fact]
        public void ConstructorWithCodeLanguageAndMarkupParserSetsPropertiesAppropriately()
        {
            // Arrange
            RazorCodeLanguage language = new CSharpRazorCodeLanguage();
            ParserBase expected = new HtmlMarkupParser();

            // Act
            RazorEngineHost host = new RazorEngineHost(language, () => expected);

            // Assert
            VerifyCommonDefaults(host);
            Assert.Same(language, host.CodeLanguage);
            Assert.Same(expected, host.CreateMarkupParser());
        }

        [Fact]
        public void DecorateCodeParserRequiresNonNullCodeParser()
        {
            Assert.ThrowsArgumentNull(() => CreateHost().DecorateCodeParser(null), "incomingCodeParser");
        }

        [Fact]
        public void DecorateMarkupParserRequiresNonNullMarkupParser()
        {
            Assert.ThrowsArgumentNull(() => CreateHost().DecorateMarkupParser(null), "incomingMarkupParser");
        }

        [Fact]
        public void DecorateCodeGeneratorRequiresNonNullCodeGenerator()
        {
            Assert.ThrowsArgumentNull(() => CreateHost().DecorateCodeGenerator(null), "incomingCodeGenerator");
        }

        [Fact]
        public void DecorateCodeParserDoesNotModifyIncomingParser()
        {
            // Arrange
            ParserBase expected = new CSharpCodeParser();

            // Act
            ParserBase actual = CreateHost().DecorateCodeParser(expected);

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void DecorateMarkupParserReturnsIncomingParser()
        {
            // Arrange
            ParserBase expected = new HtmlMarkupParser();

            // Act
            ParserBase actual = CreateHost().DecorateMarkupParser(expected);

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void DecorateCodeGeneratorReturnsIncomingCodeGenerator()
        {
            // Arrange
            RazorCodeGenerator expected = new CSharpRazorCodeGenerator("Foo", "Bar", "Baz", CreateHost());

            // Act
            RazorCodeGenerator actual = CreateHost().DecorateCodeGenerator(expected);

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
