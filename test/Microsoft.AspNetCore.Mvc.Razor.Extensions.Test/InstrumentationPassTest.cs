// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.RazorIRAssert;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class InstrumentationPassTest
    {
        [Fact]
        public void InstrumentationPass_InstrumentsHtml()
        {
            // Arrange
            var document = new DocumentIRNode();
            var builder = RazorIRBuilder.Create(document);

            builder.Push(new HtmlContentIRNode()
            {
                Source = CreateSource(1),
            });
            builder.Add(new RazorIRToken()
            {
                Content = "Hi",
                Kind = RazorIRToken.TokenKind.Html,
                Source = CreateSource(1)
            });
            builder.Pop();
            
            var pass = new InstrumentationPass()
            {
                Engine = RazorEngine.CreateEmpty(b => { }),
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), document);

            // Assert
            Children(
                document,
                n => BeginInstrumentation("1, 1, true", n),
                n => RazorIRAssert.Html("Hi", n),
                n => EndInstrumentation(n));
        }

        [Fact]
        public void InstrumentationPass_SkipsHtml_WithoutLocation()
        {
            // Arrange
            var document = new DocumentIRNode();
            var builder = RazorIRBuilder.Create(document);
            builder.Push(new HtmlContentIRNode());
            builder.Add(new RazorIRToken()
            {
                Content = "Hi",
                Kind = RazorIRToken.TokenKind.Html,
            });
            builder.Pop();

            var pass = new InstrumentationPass()
            {
                Engine = RazorEngine.CreateEmpty(b => { }),
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), document);

            // Assert
            Children(
                document,
                n => RazorIRAssert.Html("Hi", n));
        }

        [Fact]
        public void InstrumentationPass_InstrumentsCSharpExpression()
        {
            // Arrange
            var document = new DocumentIRNode();
            var builder = RazorIRBuilder.Create(document);
            builder.Push(new CSharpExpressionIRNode()
            {
                Source = CreateSource(2),
            });
            builder.Add(new RazorIRToken()
            {
                Content = "Hi",
                Kind = RazorIRToken.TokenKind.CSharp,
            });

            var pass = new InstrumentationPass()
            {
                Engine = RazorEngine.CreateEmpty(b => { }),
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), document);

            // Assert
            Children(
                document,
                n => BeginInstrumentation("2, 2, false", n),
                n => CSharpExpression("Hi", n),
                n => EndInstrumentation(n));
        }

        [Fact]
        public void InstrumentationPass_SkipsCSharpExpression_WithoutLocation()
        {
            // Arrange
            var document = new DocumentIRNode();
            var builder = RazorIRBuilder.Create(document);
            builder.Push(new CSharpExpressionIRNode());
            builder.Add(new RazorIRToken()
            {
                Content = "Hi",
                Kind = RazorIRToken.TokenKind.CSharp,
            });

            var pass = new InstrumentationPass()
            {
                Engine = RazorEngine.CreateEmpty(b => { }),
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), document);

            // Assert
            Children(
                document,
                n => CSharpExpression("Hi", n));
        }

        [Fact]
        public void InstrumentationPass_SkipsCSharpExpression_InsideTagHelperAttribute()
        {
            // Arrange
            var document = new DocumentIRNode();
            var builder = RazorIRBuilder.Create(document);
            builder.Push(new TagHelperIRNode());

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

            var pass = new InstrumentationPass()
            {
                Engine = RazorEngine.CreateEmpty(b => { }),
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), document);

            // Assert
            Children(
                document,
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
            var document = new DocumentIRNode();
            var builder = RazorIRBuilder.Create(document);
            builder.Push(new TagHelperIRNode());

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

            var pass = new InstrumentationPass()
            {
                Engine = RazorEngine.CreateEmpty(b => { }),
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), document);

            // Assert
            Children(
                document,
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
        public void InstrumentationPass_InstrumentsTagHelper()
        {
            // Arrange
            var document = new DocumentIRNode();
            var builder = RazorIRBuilder.Create(document);
            builder.Add(new TagHelperIRNode()
            {
                Source = CreateSource(3),
            });

            var pass = new InstrumentationPass()
            {
                Engine = RazorEngine.CreateEmpty(b => { }),
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), document);

            // Assert
            Children(
                document,
                n => BeginInstrumentation("3, 3, false", n),
                n => Assert.IsType<TagHelperIRNode>(n),
                n => EndInstrumentation(n));
        }

        [Fact]
        public void InstrumentationPass_SkipsTagHelper_WithoutLocation()
        {
            // Arrange
            var document = new DocumentIRNode();
            var builder = RazorIRBuilder.Create(document);
            builder.Push(new TagHelperIRNode());

            var pass = new InstrumentationPass()
            {
                Engine = RazorEngine.CreateEmpty(b => { }),
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), document);

            // Assert
            Children(
                document,
                n => Assert.IsType<TagHelperIRNode>(n));
        }

        private SourceSpan CreateSource(int number)
        {
            // The actual source span doesn't really matter, we just want to see the values used.
            return new SourceSpan(new SourceLocation(number, number, number), number);
        }
    }
}
