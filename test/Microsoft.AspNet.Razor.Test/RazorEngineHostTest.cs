// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Xunit;

namespace Microsoft.AspNet.Razor.Test
{
    public class RazorEngineHostTest
    {
        [Fact]
        public void ConstructorRequiresNonNullCodeLanguage()
        {
            Assert.Throws<ArgumentNullException>("codeLanguage", () => new RazorEngineHost(null));
            Assert.Throws<ArgumentNullException>("codeLanguage", () => new RazorEngineHost(null, () => new HtmlMarkupParser()));
        }

        [Fact]
        public void ConstructorRequiresNonNullMarkupParser()
        {
            Assert.Throws<ArgumentNullException>("markupParserFactory", () => new RazorEngineHost(new CSharpRazorCodeLanguage(), null));
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
            Assert.Throws<ArgumentNullException>("incomingCodeParser", () => CreateHost().DecorateCodeParser(null));
        }

        [Fact]
        public void DecorateMarkupParserRequiresNonNullMarkupParser()
        {
            Assert.Throws<ArgumentNullException>("incomingMarkupParser", () => CreateHost().DecorateMarkupParser(null));
        }

        [Fact]
        public void DecorateCodeGeneratorRequiresNonNullCodeGenerator()
        {
            Assert.Throws<ArgumentNullException>("incomingCodeGenerator", () => CreateHost().DecorateCodeGenerator(null));
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
