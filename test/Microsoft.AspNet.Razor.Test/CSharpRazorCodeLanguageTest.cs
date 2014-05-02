// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
