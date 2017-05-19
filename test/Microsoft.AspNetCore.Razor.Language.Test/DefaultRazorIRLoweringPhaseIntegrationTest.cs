// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.RazorIRAssert;
using Moq;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultRazorIRLoweringPhaseIntegrationTest
    {
        [Fact]
        public void Lower_EmptyDocument_AddsChecksum()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            // Act
            var irDocument = Lower(codeDocument);

            // Assert
            Children(irDocument, n => Checksum(n));
        }

        [Fact]
        public void Lower_SetsOptions_Defaults()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            // Act
            var irDocument = Lower(codeDocument);

            // Assert
            Assert.NotNull(irDocument.Options);
            Assert.False(irDocument.Options.DesignTime);
            Assert.Equal(4, irDocument.Options.IndentSize);
            Assert.False(irDocument.Options.IndentWithTabs);
        }

        [Fact]
        public void Lower_SetsOptions_RunsConfigureCallbacks()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var callback = new Mock<IRazorCodeGenerationOptionsFeature>();
            callback
                .Setup(c => c.Configure(It.IsAny<RazorCodeGenerationOptionsBuilder>()))
                .Callback<RazorCodeGenerationOptionsBuilder>(o =>
                {
                    o.DesignTime = true;
                    o.IndentSize = 17;
                    o.IndentWithTabs = true;
                });

            // Act
            var irDocument = Lower(codeDocument, builder: b =>
            {
                b.Features.Add(callback.Object);
            });

            // Assert
            Assert.NotNull(irDocument.Options);
            Assert.True(irDocument.Options.DesignTime);
            Assert.Equal(17, irDocument.Options.IndentSize);
            Assert.True(irDocument.Options.IndentWithTabs);
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
                n => Checksum(n),
                n => Html("Hello, World!", n));
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
                n => Checksum(n),
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
                n => Checksum(n),
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
                n => Checksum(n),
                n => Directive(
                    "functions",
                    n,
                    c => Assert.IsType<CSharpStatementIRNode>(c)));
        }

        [Fact]
        public void Lower_WithUsing()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create(@"@using System");
            var expectedSourceLocation = new SourceSpan(codeDocument.Source.FileName, 1, 0, 1, 12);

            // Act
            var irDocument = Lower(codeDocument);

            // Assert
            Children(irDocument,
                n => Checksum(n),
                n =>
                {
                    Using("System", n);
                    Assert.Equal(expectedSourceLocation, n.Source);
                });
        }

        [Fact]
        public void Lower_TagHelpers()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create(@"@addTagHelper *, TestAssembly
<span val=""@Hello World""></span>");
            var tagHelpers = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "span",
                    typeName: "SpanTagHelper",
                    assemblyName: "TestAssembly")
            };

            // Act
            var irDocument = Lower(codeDocument, tagHelpers: tagHelpers);

            // Assert
            Children(irDocument,
                n => Checksum(n),
                n => Directive(
                    SyntaxConstants.CSharp.AddTagHelperKeyword,
                    n,
                    v => DirectiveToken(DirectiveTokenKind.String, "*, TestAssembly", v)),
                n => TagHelperFieldDeclaration(n, "SpanTagHelper"),
                n =>
                {
                    var tagHelperNode = Assert.IsType<TagHelperIRNode>(n);
                    Children(
                        tagHelperNode,
                        c => TagHelperStructure("span", TagMode.StartTagAndEndTag, c),
                        c => Assert.IsType<CreateTagHelperIRNode>(c),
                        c => TagHelperHtmlAttribute(
                            "val",
                            HtmlAttributeValueStyle.DoubleQuotes,
                            c,
                            v => CSharpAttributeValue(string.Empty, "Hello", v),
                            v => LiteralAttributeValue(" ", "World", v)),
                        c => Assert.IsType<ExecuteTagHelpersIRNode>(c));
                });
        }

        [Fact]
        public void Lower_TagHelpers_WithPrefix()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create(@"@addTagHelper *, TestAssembly
@tagHelperPrefix cool:
<cool:span val=""@Hello World""></cool:span>");
            var tagHelpers = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "span",
                    typeName: "SpanTagHelper",
                    assemblyName: "TestAssembly")
            };

            // Act
            var irDocument = Lower(codeDocument, tagHelpers: tagHelpers);

            // Assert
            Children(irDocument,
                n => Checksum(n),
                n => Directive(
                    SyntaxConstants.CSharp.AddTagHelperKeyword,
                    n,
                    v => DirectiveToken(DirectiveTokenKind.String, "*, TestAssembly", v)),
                n => Directive(
                    SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                    n,
                    v => DirectiveToken(DirectiveTokenKind.String, "cool:", v)),
                n => TagHelperFieldDeclaration(n, "SpanTagHelper"),
                n =>
                {
                    var tagHelperNode = Assert.IsType<TagHelperIRNode>(n);
                    Children(
                        tagHelperNode,
                        c => TagHelperStructure("span", TagMode.StartTagAndEndTag, c), // Note: this is span not cool:span
                        c => Assert.IsType<CreateTagHelperIRNode>(c),
                        c => TagHelperHtmlAttribute(
                            "val",
                            HtmlAttributeValueStyle.DoubleQuotes,
                            c,
                            v => CSharpAttributeValue(string.Empty, "Hello", v),
                            v => LiteralAttributeValue(" ", "World", v)),
                        c => Assert.IsType<ExecuteTagHelpersIRNode>(c));
                });
        }

        [Fact]
        public void Lower_TagHelper_InSection()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create(@"@addTagHelper *, TestAssembly
@section test {
<span val=""@Hello World""></span>
}");
            var tagHelpers = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "span",
                    typeName: "SpanTagHelper",
                    assemblyName: "TestAssembly")
            };

            // Act
            var irDocument = Lower(codeDocument, tagHelpers: tagHelpers);

            // Assert
            Children(
                irDocument,
                n => Checksum(n),
                n => Directive(
                    SyntaxConstants.CSharp.AddTagHelperKeyword,
                    n,
                    v => DirectiveToken(DirectiveTokenKind.String, "*, TestAssembly", v)),
                n => Directive(
                    "section",
                    n,
                    c1 => DirectiveToken(DirectiveTokenKind.Member, "test", c1),
                    c1 => Html(Environment.NewLine, c1),
                    c1 =>
                    {
                        var tagHelperNode = Assert.IsType<TagHelperIRNode>(c1);
                        Children(
                            tagHelperNode,
                            c2 => TagHelperStructure("span", TagMode.StartTagAndEndTag, c2),
                            c2 => Assert.IsType<CreateTagHelperIRNode>(c2),
                            c2 => TagHelperHtmlAttribute(
                                "val",
                                HtmlAttributeValueStyle.DoubleQuotes,
                                c2,
                                v => CSharpAttributeValue(string.Empty, "Hello", v),
                                v => LiteralAttributeValue(" ", "World", v)),
                            c2 => Assert.IsType<ExecuteTagHelpersIRNode>(c2));
                    },
                    c1 => Html(Environment.NewLine, c1)),
                n => TagHelperFieldDeclaration(n, "SpanTagHelper"));
        }

        [Fact]
        public void Lower_TagHelpersWithBoundAttribute()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create(@"@addTagHelper *, TestAssembly
<input bound='foo' />");
            var tagHelpers = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes: new Action<BoundAttributeDescriptorBuilder>[]
                    {
                        builder => builder
                            .Name("bound")
                            .PropertyName("FooProp")
                            .TypeName("System.String"),
                    })
            };

            // Act
            var irDocument = Lower(codeDocument, tagHelpers: tagHelpers);

            // Assert
            Children(
                irDocument,
                n => Checksum(n),
                n => Directive(
                    SyntaxConstants.CSharp.AddTagHelperKeyword,
                    n,
                    v => DirectiveToken(DirectiveTokenKind.String, "*, TestAssembly", v)),
                n => TagHelperFieldDeclaration(n, "InputTagHelper"),
                n =>
                {
                    var tagHelperNode = Assert.IsType<TagHelperIRNode>(n);
                    Children(tagHelperNode,
                    c => TagHelperStructure("input", TagMode.SelfClosing, c),
                    c => Assert.IsType<CreateTagHelperIRNode>(c),
                    c => SetTagHelperProperty(
                        "bound",
                        "FooProp",
                        HtmlAttributeValueStyle.SingleQuotes,
                        c,
                        v => Html("foo", v)),
                    c => Assert.IsType<ExecuteTagHelpersIRNode>(c));
                });
        }

        [Fact]
        public void Lower_WithImports_Using()
        {
            // Arrange
            var source = TestRazorSourceDocument.Create(@"@using System.Threading.Tasks
<p>Hi!</p>");
            var imports = new[]
            {
                TestRazorSourceDocument.Create("@using System.Globalization"),
                TestRazorSourceDocument.Create("@using System.Text"),
            };
            
            var codeDocument = TestRazorCodeDocument.Create(source, imports);

            // Act
            var irDocument = Lower(codeDocument);

            // Assert
            Children(
                irDocument,
                n => Checksum(n),
                n => Using("System.Globalization", n),
                n => Using("System.Text", n),
                n => Using("System.Threading.Tasks", n),
                n => Html("<p>Hi!</p>", n));
        }

        [Fact]
        public void Lower_WithImports_Directive()
        {
            // Arrange
            var source = TestRazorSourceDocument.Create("<p>Hi!</p>");
            var imports = new[]
            {
                TestRazorSourceDocument.Create("@test value1"),
                TestRazorSourceDocument.Create("@test value2"),
            };

            var codeDocument = TestRazorCodeDocument.Create(source, imports);

            // Act
            var irDocument = Lower(codeDocument, b =>
            {
                b.AddDirective(DirectiveDescriptor.CreateDirective("test", DirectiveKind.SingleLine, d => d.AddMemberToken()));
            });

            // Assert
            Children(
                irDocument,
                n => Checksum(n),
                n => Directive("test", n, c => DirectiveToken(DirectiveTokenKind.Member, "value1", c)),
                n => Directive("test", n, c => DirectiveToken(DirectiveTokenKind.Member, "value2", c)),
                n => Html("<p>Hi!</p>", n));
        }

        [Fact]
        public void Lower_WithImports_IgnoresBlockDirective()
        {
            // Arrange
            var source = TestRazorSourceDocument.Create("<p>Hi!</p>");
            var imports = new[]
            {
                TestRazorSourceDocument.Create("@block token { }"),
            };

            var codeDocument = TestRazorCodeDocument.Create(source, imports);

            // Act
            var irDocument = Lower(codeDocument, b =>
            {
                b.AddDirective(DirectiveDescriptor.CreateDirective("block", DirectiveKind.RazorBlock, d => d.AddMemberToken()));
            });

            // Assert
            Children(
                irDocument,
                n => Checksum(n),
                n => Html("<p>Hi!</p>", n));
        }

        private DocumentIRNode Lower(
            RazorCodeDocument codeDocument,
            Action<IRazorEngineBuilder> builder = null,
            IEnumerable<TagHelperDescriptor> tagHelpers = null)
        {
            tagHelpers = tagHelpers ?? new TagHelperDescriptor[0];

            var engine = RazorEngine.Create(b =>
            {
                builder?.Invoke(b);

                b.AddTagHelpers(tagHelpers);
            });

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

        private static TagHelperDescriptor CreateTagHelperDescriptor(
            string tagName,
            string typeName,
            string assemblyName,
            IEnumerable<Action<BoundAttributeDescriptorBuilder>> attributes = null)
        {
            var builder = TagHelperDescriptorBuilder.Create(typeName, assemblyName);

            if (attributes != null)
            {
                foreach (var attributeBuilder in attributes)
                {
                    builder.BindAttribute(attributeBuilder);
                }
            }

            builder.TagMatchingRule(ruleBuilder => ruleBuilder.RequireTagName(tagName));

            var descriptor = builder.Build();

            return descriptor;
        }
    }
}
