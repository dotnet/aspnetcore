// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using static Microsoft.AspNetCore.Razor.Evolution.Intermediate.RazorIRAssert;
using Xunit;
using System;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public class LoweringIntegrationTest
    {
        [Fact]
        public void Lower_EmptyDocument()
        {
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var irDocument = Lower(codeDocument);

            var @namespace = SingleChild<NamespaceDeclarationIRNode>(irDocument);
            var @class = SingleChild<ClassDeclarationIRNode>(@namespace);
            var method = SingleChild<MethodDeclarationIRNode>(@class);
            var html = SingleChild<HtmlContentIRNode>(method);

            Assert.Equal(string.Empty, html.Content);
        }

        [Fact]
        public void Lower_HelloWorld()
        {
            var codeDocument = TestRazorCodeDocument.Create("Hello, World!");

            var irDocument = Lower(codeDocument);

            var @namespace = SingleChild<NamespaceDeclarationIRNode>(irDocument);
            var @class = SingleChild<ClassDeclarationIRNode>(@namespace);
            var method = SingleChild<MethodDeclarationIRNode>(@class);
            var html = SingleChild<HtmlContentIRNode>(method);

            Assert.Equal("Hello, World!", html.Content);
        }

        [Fact]
        public void Lower_HtmlWithAttributes()
        {
            var codeDocument = TestRazorCodeDocument.Create(@"
<html>
    <body>
        <span data-val=""@Hello"" />
    </body>
</html>");
            var irDocument = Lower(codeDocument);
            
            var @namespace = SingleChild<NamespaceDeclarationIRNode>(irDocument);
            var @class = SingleChild<ClassDeclarationIRNode>(@namespace);
            var method = SingleChild<MethodDeclarationIRNode>(@class);
            Children(method,
                n => Html(Environment.NewLine, n),
                n => Html("<html>", n),
                n => Html(Environment.NewLine + "    ", n),
                n => Html("<body>", n),
                n => Html(Environment.NewLine + "        ", n),
                n => Html("<span", n),
                n => Html(" data-val=\"", n),
                n => CSharpExpression("Hello", n),
                n => Html("\"", n),
                n => Html(" />", n),
                n => Html(Environment.NewLine + "    ", n),
                n => Html("</body>", n),
                n => Html(Environment.NewLine, n),
                n => Html("</html>", n));
        }

        [Fact]
        public void Lower_WithUsing()
        {
            var codeDocument = TestRazorCodeDocument.Create(@"@functions { public int Foo { get; set; }}");
            var irDocument = Lower(codeDocument);

            var @namespace = SingleChild<NamespaceDeclarationIRNode>(irDocument);
            var @class = SingleChild<ClassDeclarationIRNode>(@namespace);
            Children(@class,
                n => Assert.IsType<MethodDeclarationIRNode>(n),
                n => Assert.IsType<CSharpStatementIRNode>(n));
        }

        [Fact]
        public void Lower_WithFunctions()
        {
            var codeDocument = TestRazorCodeDocument.Create(@"@using System");
            var irDocument = Lower(codeDocument);

            var @namespace = SingleChild<NamespaceDeclarationIRNode>(irDocument);
            Children(@namespace,
                n => Using("using System", n),
                n => Assert.IsType<ClassDeclarationIRNode>(n));
        }

        private DocumentIRNode Lower(RazorCodeDocument codeDocument)
        {
            var engine = RazorEngine.Create();

            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorIRLoweringPhase)
                {
                    break;
                }
            }

            var irDocument = codeDocument.GetIRDocument();
            Assert.NotNull(irDocument);
            return irDocument;
        }
    }
}
