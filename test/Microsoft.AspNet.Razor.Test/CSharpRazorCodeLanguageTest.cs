// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.CSharp;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test
{
    public class CSharpRazorCodeLanguageTest
    {
        [Fact]
        public void CreateCodeParserReturnsNewCSharpCodeParser()
        {
            // Arrange
            RazorCodeLanguage service = new CSharpRazorCodeLanguage();

            // Act
            ParserBase parser = service.CreateCodeParser();

            // Assert
            Assert.NotNull(parser);
            Assert.IsType<CSharpCodeParser>(parser);
        }

        [Fact]
        public void CreateCodeGeneratorParserListenerReturnsNewCSharpCodeGeneratorParserListener()
        {
            // Arrange
            RazorCodeLanguage service = new CSharpRazorCodeLanguage();

            // Act
            RazorEngineHost host = new RazorEngineHost(service);
            RazorCodeGenerator generator = service.CreateCodeGenerator("Foo", "Bar", "Baz", host);

            // Assert
            Assert.NotNull(generator);
            Assert.IsType<CSharpRazorCodeGenerator>(generator);
            Assert.Equal("Foo", generator.ClassName);
            Assert.Equal("Bar", generator.RootNamespaceName);
            Assert.Equal("Baz", generator.SourceFileName);
            Assert.Same(host, generator.Host);
        }
    }
}
