// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpAutoCompleteTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void FunctionsDirectiveAutoCompleteAtEOF()
        {
            // Arrange, Act & Assert
            ParseBlockTest("@functions{", new[] { FunctionsDirective.Directive });
        }

        [Fact]
        public void SectionDirectiveAutoCompleteAtEOF()
        {
            // Arrange, Act & Assert
            ParseBlockTest("@section Header {", new[] { SectionDirective.Directive });
        }

        [Fact]
        public void VerbatimBlockAutoCompleteAtEOF()
        {
            ParseBlockTest("@{");
        }

        [Fact]
        public void FunctionsDirectiveAutoCompleteAtStartOfFile()
        {
            // Arrange, Act & Assert
            ParseBlockTest("@functions{" + Environment.NewLine + "foo", new[] { FunctionsDirective.Directive });
        }

        [Fact]
        public void SectionDirectiveAutoCompleteAtStartOfFile()
        {
            // Arrange, Act & Assert
            ParseBlockTest("@section Header {" + Environment.NewLine + "<p>Foo</p>", new[] { SectionDirective.Directive });
        }

        [Fact]
        public void VerbatimBlockAutoCompleteAtStartOfFile()
        {
            ParseBlockTest("@{" + Environment.NewLine + "<p></p>");
        }
    }
}
