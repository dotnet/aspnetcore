// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Moq;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.IntermediateNodeAssert;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultRazorIntermediateNodeLoweringPhaseIntegrationTest
    {
        [Fact]
        public void Lower_SetsOptions_Defaults()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            // Act
            var documentNode = Lower(codeDocument);

            // Assert
            Assert.NotNull(documentNode.Options);
            Assert.False(documentNode.Options.DesignTime);
            Assert.Equal(4, documentNode.Options.IndentSize);
            Assert.False(documentNode.Options.IndentWithTabs);
        }

        [Fact]
        public void Lower_SetsOptions_RunsConfigureCallbacks()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var callback = new Mock<IConfigureRazorCodeGenerationOptionsFeature>();
            callback
                .Setup(c => c.Configure(It.IsAny<RazorCodeGenerationOptionsBuilder>()))
                .Callback<RazorCodeGenerationOptionsBuilder>(o =>
                {
                    o.IndentSize = 17;
                    o.IndentWithTabs = true;
                    o.SuppressChecksum = true;
                });

            // Act
            var documentNode = Lower(
                codeDocument,
                builder: b =>
                {
                    b.Features.Add(callback.Object);
                },
                designTime: true);

            // Assert
            Assert.NotNull(documentNode.Options);
            Assert.True(documentNode.Options.DesignTime);
            Assert.Equal(17, documentNode.Options.IndentSize);
            Assert.True(documentNode.Options.IndentWithTabs);
            Assert.True(documentNode.Options.SuppressChecksum);
        }

        [Fact]
        public void Lower_HelloWorld()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create("Hello, World!");

            // Act
            var documentNode = Lower(codeDocument);

            // Assert
            Children(documentNode,
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
            var documentNode = Lower(codeDocument);

            // Assert
            Children(documentNode,
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
            var documentNode = Lower(codeDocument);

            // Assert
            Children(documentNode,
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
                    valueValidators: new Action<IntermediateNode>[]
                    {
                        value => CSharpExpressionAttributeValue(string.Empty, "Hello", value),
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
            var documentNode = Lower(codeDocument);

            // Assert
            Children(documentNode,
                n => Directive(
                    "functions",
                    n,
                    c => Assert.IsType<CSharpCodeIntermediateNode>(c)));
        }

        [Fact]
        public void Lower_WithUsing()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create(@"@using System");
            var expectedSourceLocation = new SourceSpan(codeDocument.Source.FilePath, 1, 0, 1, 12);

            // Act
            var documentNode = Lower(codeDocument);

            // Assert
            Children(documentNode,
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
            var documentNode = Lower(codeDocument, tagHelpers: tagHelpers);

            // Assert
            Children(documentNode,
                n => Directive(
                    SyntaxConstants.CSharp.AddTagHelperKeyword,
                    n,
                    v => DirectiveToken(DirectiveTokenKind.String, "*, TestAssembly", v)),
                n => TagHelper(
                    "span",
                    TagMode.StartTagAndEndTag,
                    tagHelpers,
                    n,
                    c => Assert.IsType<TagHelperBodyIntermediateNode>(c),
                    c => TagHelperHtmlAttribute(
                        "val",
                        AttributeStructure.DoubleQuotes,
                        c,
                        v => CSharpExpressionAttributeValue(string.Empty, "Hello", v),
                        v => LiteralAttributeValue(" ", "World", v))));
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
            var documentNode = Lower(codeDocument, tagHelpers: tagHelpers);

            // Assert
            Children(documentNode,
                n => Directive(
                    SyntaxConstants.CSharp.AddTagHelperKeyword,
                    n,
                    v => DirectiveToken(DirectiveTokenKind.String, "*, TestAssembly", v)),
                n => Directive(
                    SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                    n,
                    v => DirectiveToken(DirectiveTokenKind.String, "cool:", v)),
                n => TagHelper(
                    "span",  // Note: this is span not cool:span
                    TagMode.StartTagAndEndTag,
                    tagHelpers,
                    n,
                    c => Assert.IsType<TagHelperBodyIntermediateNode>(c),
                    c => TagHelperHtmlAttribute(
                        "val",
                        AttributeStructure.DoubleQuotes,
                        c,
                        v => CSharpExpressionAttributeValue(string.Empty, "Hello", v),
                        v => LiteralAttributeValue(" ", "World", v))));
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
            var documentNode = Lower(codeDocument, tagHelpers: tagHelpers);

            // Assert
            Children(
                documentNode,
                n => Directive(
                    SyntaxConstants.CSharp.AddTagHelperKeyword,
                    n,
                    v => DirectiveToken(DirectiveTokenKind.String, "*, TestAssembly", v)),
                n => Directive(
                    "section",
                    n,
                    c1 => DirectiveToken(DirectiveTokenKind.Member, "test", c1),
                    c1 => Html(Environment.NewLine, c1),
                    c1 => TagHelper(
                        "span",
                        TagMode.StartTagAndEndTag,
                        tagHelpers,
                        c1,
                        c2 => Assert.IsType<TagHelperBodyIntermediateNode>(c2),
                        c2 => TagHelperHtmlAttribute(
                            "val",
                            AttributeStructure.DoubleQuotes,
                            c2,
                            v => CSharpExpressionAttributeValue(string.Empty, "Hello", v),
                            v => LiteralAttributeValue(" ", "World", v))),
                    c1 => Html(Environment.NewLine, c1)));
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
            var documentNode = Lower(codeDocument, tagHelpers: tagHelpers);

            // Assert
            Children(
                documentNode,
                n => Directive(
                    SyntaxConstants.CSharp.AddTagHelperKeyword,
                    n,
                    v => DirectiveToken(DirectiveTokenKind.String, "*, TestAssembly", v)),
                n => TagHelper(
                    "input",
                    TagMode.SelfClosing,
                    tagHelpers,
                    n,
                    c => Assert.IsType<TagHelperBodyIntermediateNode>(c),
                    c => SetTagHelperProperty(
                        "bound",
                        "FooProp",
                        AttributeStructure.SingleQuotes,
                        c,
                        v => Html("foo", v))));
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
            var documentNode = Lower(codeDocument);

            // Assert
            Children(
                documentNode,
                n => Using("System.Globalization", n),
                n => Using("System.Text", n),
                n => Using("System.Threading.Tasks", n),
                n => Html("<p>Hi!</p>", n));
        }

        [Fact]
        public void Lower_WithImports_AllowsIdenticalNamespacesInPrimaryDocument()
        {
            // Arrange
            var source = TestRazorSourceDocument.Create(@"@using System.Threading.Tasks
@using System.Threading.Tasks");
            var imports = new[]
            {
                TestRazorSourceDocument.Create("@using System.Threading.Tasks"),
            };

            var codeDocument = TestRazorCodeDocument.Create(source, imports);

            // Act
            var documentNode = Lower(codeDocument);

            // Assert
            Children(
                documentNode,
                n => Using("System.Threading.Tasks", n),
                n => Using("System.Threading.Tasks", n));
        }

        [Fact]
        public void Lower_WithMultipleImports_SingleLineFileScopedSinglyOccurring()
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
            var documentNode = Lower(codeDocument, b =>
            {
                b.AddDirective(DirectiveDescriptor.CreateDirective(
                    "test",
                    DirectiveKind.SingleLine,
                    builder =>
                    {
                        builder.AddMemberToken();
                        builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
                    }));
            });

            // Assert
            Children(
                documentNode,
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
            var documentNode = Lower(codeDocument, b =>
            {
                b.AddDirective(DirectiveDescriptor.CreateDirective("block", DirectiveKind.RazorBlock, d => d.AddMemberToken()));
            });

            // Assert
            Children(
                documentNode,
                n => Html("<p>Hi!</p>", n));
        }

        private DocumentIntermediateNode Lower(
            RazorCodeDocument codeDocument,
            Action<RazorProjectEngineBuilder> builder = null,
            IEnumerable<TagHelperDescriptor> tagHelpers = null,
            bool designTime = false)
        {
            tagHelpers = tagHelpers ?? new TagHelperDescriptor[0];

            Action<RazorProjectEngineBuilder> configureEngine = b =>
            {
                builder?.Invoke(b);

                SectionDirective.Register(b);
                b.AddTagHelpers(tagHelpers);

                b.Features.Add(new DesignTimeOptionsFeature(designTime));
            };

            var projectEngine = RazorProjectEngine.Create(configureEngine);

            for (var i = 0; i < projectEngine.Phases.Count; i++)
            {
                var phase = projectEngine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorIntermediateNodeLoweringPhase)
                {
                    break;
                }
            }

            var documentNode = codeDocument.GetDocumentIntermediateNode();
            Assert.NotNull(documentNode);

            return documentNode;
        }

        private static TagHelperDescriptor CreateTagHelperDescriptor(
            string tagName,
            string typeName,
            string assemblyName,
            IEnumerable<Action<BoundAttributeDescriptorBuilder>> attributes = null)
        {
            var builder = TagHelperDescriptorBuilder.Create(typeName, assemblyName);
            builder.TypeName(typeName);

            if (attributes != null)
            {
                foreach (var attributeBuilder in attributes)
                {
                    builder.BoundAttributeDescriptor(attributeBuilder);
                }
            }

            builder.TagMatchingRuleDescriptor(ruleBuilder => ruleBuilder.RequireTagName(tagName));

            var descriptor = builder.Build();

            return descriptor;
        }

        private class DesignTimeOptionsFeature : IConfigureRazorParserOptionsFeature, IConfigureRazorCodeGenerationOptionsFeature
        {
            private bool _designTime;

            public DesignTimeOptionsFeature(bool designTime)
            {
                _designTime = designTime;
            }

            public int Order { get; }

            public RazorEngine Engine { get; set; }

            public void Configure(RazorParserOptionsBuilder options)
            {
                options.SetDesignTime(_designTime);
            }

            public void Configure(RazorCodeGenerationOptionsBuilder options)
            {
                options.SetDesignTime(_designTime);
            }
        }
    }
}
