// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using Xunit;
using static Microsoft.AspNetCore.Razor.Evolution.Intermediate.RazorIRAssert;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public class DefaultRazorIRLoweringPhaseIntegrationTest
    {
        [Fact]
        public void Lower_EmptyDocument_AddsGlobalUsingsAndNamespace()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            // Act
            var irDocument = Lower(codeDocument);

            // Assert
            Children(irDocument,
                n => Assert.IsType<ChecksumIRNode>(n),
                n => Using("System", n),
                n => Using("System.Threading.Tasks", n));
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
                n => Assert.IsType<UsingStatementIRNode>(n),
                n => Assert.IsType<UsingStatementIRNode>(n),
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
                n => Assert.IsType<ChecksumIRNode>(n),
                n => Assert.IsType<UsingStatementIRNode>(n),
                n => Assert.IsType<UsingStatementIRNode>(n),
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
                n => Assert.IsType<UsingStatementIRNode>(n),
                n => Assert.IsType<UsingStatementIRNode>(n),
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
                n => Assert.IsType<UsingStatementIRNode>(n),
                n => Assert.IsType<UsingStatementIRNode>(n),
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

            // Act
            var irDocument = Lower(codeDocument);

            // Assert
            Children(irDocument,
                n => Assert.IsType<ChecksumIRNode>(n),
                n => Using("System", n),
                n => Using(typeof(Task).Namespace, n));
        }

        [Fact]
        public void Lower_TagHelpers()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create(@"<span val=""@Hello World""></span>");
            var descriptors = new[]
            {
                new TagHelperDescriptor
                {
                    TagName = "span",
                    TypeName = "SpanTagHelper"
                }
            };

            // Act
            var irDocument = Lower(codeDocument, descriptors);

            // Assert
            Children(irDocument,
                n => Assert.IsType<ChecksumIRNode>(n),
                n => Using("System", n),
                n => Using(typeof(Task).Namespace, n),
                n => TagHelperFieldDeclaration(n, "SpanTagHelper"),
                n =>
                {
                    var tagHelperNode = Assert.IsType<TagHelperIRNode>(n);
                    Children(tagHelperNode,
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
        public void Lower_TagHelpersWithBoundAttribute()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create("<input bound='foo' />");
            var descriptor = new TagHelperDescriptor
            {
                TagName = "input",
                TypeName = "InputTagHelper",
                Attributes = new[] { new TagHelperAttributeDescriptor
                {
                    Name = "bound",
                    PropertyName = "FooProp",
                    TypeName = "System.String"
                } }
            };

            // Act
            var irDocument = Lower(codeDocument, new[] { descriptor });

            // Assert
            Children(irDocument,
                n => Assert.IsType<ChecksumIRNode>(n),
                n => Using("System", n),
                n => Using(typeof(Task).Namespace, n),
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

        private DocumentIRNode Lower(RazorCodeDocument codeDocument)
        {
            return Lower(codeDocument, Enumerable.Empty<TagHelperDescriptor>());
        }

        private DocumentIRNode Lower(RazorCodeDocument codeDocument, IEnumerable<TagHelperDescriptor> descriptors)
        {
            var engine = RazorEngine.Create(builder =>
            {
                builder.Features.Add(new TagHelperFeature(new TestTagHelperDescriptorResolver(descriptors)));
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

        private class TestTagHelperDescriptorResolver : ITagHelperDescriptorResolver
        {
            private readonly IEnumerable<TagHelperDescriptor> _descriptors;

            public TestTagHelperDescriptorResolver(IEnumerable<TagHelperDescriptor> descriptors)
            {
                _descriptors = descriptors;
            }

            public IEnumerable<TagHelperDescriptor> Resolve(TagHelperDescriptorResolutionContext resolutionContext)
            {
                return _descriptors;
            }
        }
    }
}
