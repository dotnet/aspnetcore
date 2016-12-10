// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using static Microsoft.AspNetCore.Razor.Evolution.Intermediate.RazorIRAssert;
using Xunit;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public class DefaultRazorIRLoweringPhaseIntegrationTest
    {
        [Fact]
        public void Lower_EmptyDocument()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            // Act
            var irDocument = Lower(codeDocument);

            // Assert
            Children(irDocument,
                n => Assert.IsType<ChecksumIRNode>(n),
                n => Assert.IsType<NamespaceDeclarationIRNode>(n));
            var @namespace = irDocument.Children[1];
            Children(@namespace,
                n => Assert.IsType<UsingStatementIRNode>(n),
                n => Assert.IsType<UsingStatementIRNode>(n),
                n => Assert.IsType<ClassDeclarationIRNode>(n));
            var @class = @namespace.Children[2];
            var method = SingleChild<RazorMethodDeclarationIRNode>(@class);
            var html = SingleChild<HtmlContentIRNode>(method);

            Assert.Equal(string.Empty, html.Content);
        }

        [Fact]
        public void Lower_HelloWorld()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create("Hello, World!");

            // Act
            var irDocument = Lower(codeDocument);

            // Assert
            Children(irDocument,
                n => Assert.IsType<ChecksumIRNode>(n),
                n => Assert.IsType<NamespaceDeclarationIRNode>(n));
            var @namespace = irDocument.Children[1];
            Children(@namespace,
                n => Assert.IsType<UsingStatementIRNode>(n),
                n => Assert.IsType<UsingStatementIRNode>(n),
                n => Assert.IsType<ClassDeclarationIRNode>(n));
            var @class = @namespace.Children[2];
            var method = SingleChild<RazorMethodDeclarationIRNode>(@class);
            var html = SingleChild<HtmlContentIRNode>(method);

            Assert.Equal("Hello, World!", html.Content);
        }

        [Fact]
        public void Lower_HtmlWithDataDashAttributes()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create(@"
<html>
    <body>
        <span data-val=""@Hello"" />
    </body>
</html>");

            // Act
            var irDocument = Lower(codeDocument);

            // Assert
            Children(irDocument,
                n => Assert.IsType<ChecksumIRNode>(n),
                n => Assert.IsType<NamespaceDeclarationIRNode>(n));
            var @namespace = irDocument.Children[1];
            Children(@namespace,
                n => Assert.IsType<UsingStatementIRNode>(n),
                n => Assert.IsType<UsingStatementIRNode>(n),
                n => Assert.IsType<ClassDeclarationIRNode>(n));
            var @class = @namespace.Children[2];
            var method = SingleChild<RazorMethodDeclarationIRNode>(@class);
            Children(method,
                n => Html(
@"
<html>
    <body>
        <span data-val=""", n),
                n => CSharpExpression("Hello", n),
                n => Html(@""" />
    </body>
</html>", n));
        }

        [Fact]
        public void Lower_HtmlWithConditionalAttributes()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create(@"
<html>
    <body>
        <span val=""@Hello World"" />
    </body>
</html>");

            // Act
            var irDocument = Lower(codeDocument);

            // Assert
            Children(irDocument,
                n => Assert.IsType<ChecksumIRNode>(n),
                n => Assert.IsType<NamespaceDeclarationIRNode>(n));
            var @namespace = irDocument.Children[1];
            Children(@namespace,
                n => Assert.IsType<UsingStatementIRNode>(n),
                n => Assert.IsType<UsingStatementIRNode>(n),
                n => Assert.IsType<ClassDeclarationIRNode>(n));
            var @class = @namespace.Children[2];
            var method = SingleChild<RazorMethodDeclarationIRNode>(@class);
            Children(method,
                n => Html(
@"
<html>
    <body>
        <span", n),

                n => ConditionalAttribute(
                    prefix: " val=\"",
                    name: "val",
                    suffix: "\"",
                    node: n,
                    valueValidators: new Action<RazorIRNode>[]
                    {
                        value => CSharpAttributeValue(string.Empty, "Hello", value),
                        value => LiteralAttributeValue(" ",  "World", value)
                    }),
                n => Html(@" />
    </body>
</html>", n));
        }

        [Fact]
        public void Lower_WithFunctions()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create(@"@functions { public int Foo { get; set; }}");

            // Act
            var irDocument = Lower(codeDocument);

            // Assert
            Children(irDocument,
                n => Assert.IsType<ChecksumIRNode>(n),
                n => Assert.IsType<NamespaceDeclarationIRNode>(n));
            var @namespace = irDocument.Children[1];
            Children(@namespace,
                n => Assert.IsType<UsingStatementIRNode>(n),
                n => Assert.IsType<UsingStatementIRNode>(n),
                n => Assert.IsType<ClassDeclarationIRNode>(n));
            var @class = @namespace.Children[2];
            Children(@class,
                n => Assert.IsType<RazorMethodDeclarationIRNode>(n),
                n => Assert.IsType<CSharpStatementIRNode>(n));
        }

        [Fact]
        public void Lower_WithUsing()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create(@"@using System");

            // Act
            var irDocument = Lower(codeDocument);

            // Assert
            Children(irDocument,
                n => Assert.IsType<ChecksumIRNode>(n),
                n => Assert.IsType<NamespaceDeclarationIRNode>(n));
            var @namespace = irDocument.Children[1];
            Children(@namespace,
                n => Using("System", n),
                n => Using(typeof(Task).Namespace, n),
                n => Assert.IsType<ClassDeclarationIRNode>(n));
        }

        private DocumentIRNode Lower(RazorCodeDocument codeDocument)
        {
            var engine = RazorEngine.Create();

            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorIRPhase)
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
