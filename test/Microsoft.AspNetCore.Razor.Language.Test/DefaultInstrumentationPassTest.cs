// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.RazorIRAssert;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultInstrumentationPassTest
    {
        [Fact]
        public void InstrumentationPass_InstrumentsHtml()
        {
            // Arrange
            var builder = RazorIRBuilder.Document();
            builder.Add(new HtmlContentIRNode()
            {
                Content = "Hi",
                Source = CreateSource(1),
            });

            var irDocument = (DocumentIRNode)builder.Build();

            var pass = new DefaultInstrumentationPass();

            // Act
            pass.ExecuteCore(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Children(
                irDocument,
                n => BeginInstrumentation("1, 1, true", n),
                n => Html("Hi", n),
                n => EndInstrumentation(n));
        }

        [Fact]
        public void InstrumentationPass_SkipsHtml_WithoutLocation()
        {
            // Arrange
            var builder = RazorIRBuilder.Document();
            builder.Add(new HtmlContentIRNode()
            {
                Content = "Hi",
            });

            var irDocument = (DocumentIRNode)builder.Build();

            var pass = new DefaultInstrumentationPass();

            // Act
            pass.ExecuteCore(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Children(
                irDocument,
                n => Html("Hi", n));
        }

        [Fact]
        public void InstrumentationPass_InstrumentsCSharpExpression()
        {
            // Arrange
            var builder = RazorIRBuilder.Document();
            builder.Push(new CSharpExpressionIRNode()
            {
                Source = CreateSource(2),
            });
            builder.Add(new RazorIRToken()
            {
                Content = "Hi",
                Kind = RazorIRToken.TokenKind.CSharp,
            });

            var irDocument = (DocumentIRNode)builder.Build();

            var pass = new DefaultInstrumentationPass();

            // Act
            pass.ExecuteCore(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Children(
                irDocument,
                n => BeginInstrumentation("2, 2, false", n),
                n => CSharpExpression("Hi", n),
                n => EndInstrumentation(n));
        }

        [Fact]
        public void InstrumentationPass_SkipsCSharpExpression_WithoutLocation()
        {
            // Arrange
            var builder = RazorIRBuilder.Document();
            builder.Push(new CSharpExpressionIRNode());
            builder.Add(new RazorIRToken()
            {
                Content = "Hi",
                Kind = RazorIRToken.TokenKind.CSharp,
            });

            var irDocument = (DocumentIRNode)builder.Build();

            var pass = new DefaultInstrumentationPass();

            // Act
            pass.ExecuteCore(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Children(
                irDocument,
                n => CSharpExpression("Hi", n));
        }

        [Fact]
        public void InstrumentationPass_SkipsCSharpExpression_InsideTagHelperAttribute()
        {
            // Arrange
            var builder = RazorIRBuilder.Document();
            builder.Push(new TagHelperIRNode()
            {
                Source = CreateSource(3)
            });

            builder.Push(new AddTagHelperHtmlAttributeIRNode());

            builder.Push(new CSharpExpressionIRNode()
            {
                Source = CreateSource(5)
            });

            builder.Add(new RazorIRToken()
            {
                Content = "Hi",
                Kind = RazorIRToken.TokenKind.CSharp,
            });

            var irDocument = (DocumentIRNode)builder.Build();

            var pass = new DefaultInstrumentationPass();

            // Act
            pass.ExecuteCore(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Children(
                irDocument,
                n =>
                {
                    Assert.IsType<TagHelperIRNode>(n);
                    Children(
                        n,
                        c =>
                        {
                            Assert.IsType<AddTagHelperHtmlAttributeIRNode>(c);
                            Children(
                                c,
                                s => CSharpExpression("Hi", s));
                        });
                });
        }

        [Fact]
        public void InstrumentationPass_SkipsCSharpExpression_InsideTagHelperProperty()
        {
            // Arrange
            var builder = RazorIRBuilder.Document();
            builder.Push(new TagHelperIRNode()
            {
                Source = CreateSource(3)
            });

            builder.Push(new SetTagHelperPropertyIRNode());

            builder.Push(new CSharpExpressionIRNode()
            {
                Source = CreateSource(5)
            });

            builder.Add(new RazorIRToken()
            {
                Content = "Hi",
                Kind = RazorIRToken.TokenKind.CSharp,
            });

            var irDocument = (DocumentIRNode)builder.Build();

            var pass = new DefaultInstrumentationPass();

            // Act
            pass.ExecuteCore(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Children(
                irDocument,
                n =>
                {
                    Assert.IsType<TagHelperIRNode>(n);
                    Children(
                        n,
                        c =>
                        {
                            Assert.IsType<SetTagHelperPropertyIRNode>(c);
                            Children(
                                c,
                                s => CSharpExpression("Hi", s));
                        });
                });
        }

        [Fact]
        public void InstrumentationPass_InstrumentsExecuteTagHelper_InsideTagHelper()
        {
            // Arrange
            var builder = RazorIRBuilder.Document();
            builder.Push(new TagHelperIRNode()
            {
                Source = CreateSource(3),
            });

            builder.Add(new ExecuteTagHelpersIRNode());

            var irDocument = (DocumentIRNode)builder.Build();

            var pass = new DefaultInstrumentationPass();

            // Act
            pass.ExecuteCore(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Children(
                irDocument,
                n =>
                {
                    Assert.IsType<TagHelperIRNode>(n);
                    Children(
                        n,
                        c => BeginInstrumentation("3, 3, false", c),
                        c => Assert.IsType<ExecuteTagHelpersIRNode>(c),
                        c => EndInstrumentation(c));
                });
        }

        [Fact]
        public void InstrumentationPass_SkipsExecuteTagHelper_WithoutLocation()
        {
            // Arrange
            var builder = RazorIRBuilder.Document();
            builder.Push(new TagHelperIRNode());

            builder.Add(new ExecuteTagHelpersIRNode());

            var irDocument = (DocumentIRNode)builder.Build();

            var pass = new DefaultInstrumentationPass();

            // Act
            pass.ExecuteCore(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Children(
                irDocument,
                n =>
                {
                    Assert.IsType<TagHelperIRNode>(n);
                    Children(
                        n,
                        c => Assert.IsType<ExecuteTagHelpersIRNode>(c));
                });
        }

        [Fact]
        public void InstrumentationPass_SkipsExecuteTagHelper_MalformedTagHelper()
        {
            // Arrange
            var builder = RazorIRBuilder.Document();
            builder.Push(new TagHelperIRNode()
            {
                Source = CreateSource(3),
            });
            builder.Push(new CSharpExpressionIRNode());
            builder.Add(new ExecuteTagHelpersIRNode()); // Malformed

            var irDocument = (DocumentIRNode)builder.Build();

            var pass = new DefaultInstrumentationPass();

            // Act
            pass.ExecuteCore(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Children(
                irDocument,
                n =>
                {
                    Assert.IsType<TagHelperIRNode>(n);
                    Children(
                        n,
                        c => SingleChild<ExecuteTagHelpersIRNode>(Assert.IsType<CSharpExpressionIRNode>(c)));
                });
        }

        private SourceSpan CreateSource(int number)
        {
            // The actual source span doesn't really matter, we just want to see the values used.
            return new SourceSpan(new SourceLocation(number, number, number), number);
        }
    }
}
