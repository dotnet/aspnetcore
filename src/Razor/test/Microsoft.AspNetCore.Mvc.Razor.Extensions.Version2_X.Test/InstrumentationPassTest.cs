// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.IntermediateNodeAssert;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X
{
    public class InstrumentationPassTest
    {
        [Fact]
        public void InstrumentationPass_NoOps_ForDesignTime()
        {
            // Arrange
            var document = new DocumentIntermediateNode()
            {
                Options = RazorCodeGenerationOptions.CreateDesignTimeDefault(),
            };

            var builder = IntermediateNodeBuilder.Create(document);
            builder.Push(new HtmlContentIntermediateNode());
            builder.Add(new IntermediateToken()
            {
                Content = "Hi",
                Kind = TokenKind.Html,
            });
            builder.Pop();

            var pass = new InstrumentationPass()
            {
                Engine = RazorProjectEngine.CreateEmpty().Engine,
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), document);

            // Assert
            Children(
                document,
                n => IntermediateNodeAssert.Html("Hi", n));
        }

        [Fact]
        public void InstrumentationPass_InstrumentsHtml()
        {
            // Arrange
            var document = new DocumentIntermediateNode()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var builder = IntermediateNodeBuilder.Create(document);

            builder.Push(new HtmlContentIntermediateNode()
            {
                Source = CreateSource(1),
            });
            builder.Add(new IntermediateToken()
            {
                Content = "Hi",
                Kind = TokenKind.Html,
                Source = CreateSource(1)
            });
            builder.Pop();
            
            var pass = new InstrumentationPass()
            {
                Engine = RazorProjectEngine.CreateEmpty().Engine,
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), document);

            // Assert
            Children(
                document,
                n => BeginInstrumentation("1, 1, true", n),
                n => IntermediateNodeAssert.Html("Hi", n),
                n => EndInstrumentation(n));
        }

        [Fact]
        public void InstrumentationPass_SkipsHtml_WithoutLocation()
        {
            // Arrange
            var document = new DocumentIntermediateNode()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var builder = IntermediateNodeBuilder.Create(document);
            builder.Push(new HtmlContentIntermediateNode());
            builder.Add(new IntermediateToken()
            {
                Content = "Hi",
                Kind = TokenKind.Html,
            });
            builder.Pop();

            var pass = new InstrumentationPass()
            {
                Engine = RazorProjectEngine.CreateEmpty().Engine,
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), document);

            // Assert
            Children(
                document,
                n => IntermediateNodeAssert.Html("Hi", n));
        }

        [Fact]
        public void InstrumentationPass_InstrumentsCSharpExpression()
        {
            // Arrange
            var document = new DocumentIntermediateNode()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var builder = IntermediateNodeBuilder.Create(document);
            builder.Push(new CSharpExpressionIntermediateNode()
            {
                Source = CreateSource(2),
            });
            builder.Add(new IntermediateToken()
            {
                Content = "Hi",
                Kind = TokenKind.CSharp,
            });

            var pass = new InstrumentationPass()
            {
                Engine = RazorProjectEngine.CreateEmpty().Engine,
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
            var document = new DocumentIntermediateNode()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var builder = IntermediateNodeBuilder.Create(document);
            builder.Push(new CSharpExpressionIntermediateNode());
            builder.Add(new IntermediateToken()
            {
                Content = "Hi",
                Kind = TokenKind.CSharp,
            });

            var pass = new InstrumentationPass()
            {
                Engine = RazorProjectEngine.CreateEmpty().Engine,
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
            var document = new DocumentIntermediateNode()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var builder = IntermediateNodeBuilder.Create(document);
            builder.Push(new TagHelperIntermediateNode());

            builder.Push(new TagHelperHtmlAttributeIntermediateNode());

            builder.Push(new CSharpExpressionIntermediateNode()
            {
                Source = CreateSource(5)
            });

            builder.Add(new IntermediateToken()
            {
                Content = "Hi",
                Kind = TokenKind.CSharp,
            });

            var pass = new InstrumentationPass()
            {
                Engine = RazorProjectEngine.CreateEmpty().Engine,
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), document);

            // Assert
            Children(
                document,
                n =>
                {
                    Assert.IsType<TagHelperIntermediateNode>(n);
                    Children(
                        n,
                        c =>
                        {
                            Assert.IsType<TagHelperHtmlAttributeIntermediateNode>(c);
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
            var document = new DocumentIntermediateNode()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var builder = IntermediateNodeBuilder.Create(document);
            builder.Push(new TagHelperIntermediateNode());

            builder.Push(new TagHelperPropertyIntermediateNode());

            builder.Push(new CSharpExpressionIntermediateNode()
            {
                Source = CreateSource(5)
            });

            builder.Add(new IntermediateToken()
            {
                Content = "Hi",
                Kind = TokenKind.CSharp,
            });

            var pass = new InstrumentationPass()
            {
                Engine = RazorProjectEngine.CreateEmpty().Engine,
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), document);

            // Assert
            Children(
                document,
                n =>
                {
                    Assert.IsType<TagHelperIntermediateNode>(n);
                    Children(
                        n,
                        c =>
                        {
                            Assert.IsType<TagHelperPropertyIntermediateNode>(c);
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
            var document = new DocumentIntermediateNode()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var builder = IntermediateNodeBuilder.Create(document);
            builder.Add(new TagHelperIntermediateNode()
            {
                Source = CreateSource(3),
            });

            var pass = new InstrumentationPass()
            {
                Engine = RazorProjectEngine.CreateEmpty().Engine,
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), document);

            // Assert
            Children(
                document,
                n => BeginInstrumentation("3, 3, false", n),
                n => Assert.IsType<TagHelperIntermediateNode>(n),
                n => EndInstrumentation(n));
        }

        [Fact]
        public void InstrumentationPass_SkipsTagHelper_WithoutLocation()
        {
            // Arrange
            var document = new DocumentIntermediateNode()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var builder = IntermediateNodeBuilder.Create(document);
            builder.Push(new TagHelperIntermediateNode());

            var pass = new InstrumentationPass()
            {
                Engine = RazorProjectEngine.CreateEmpty().Engine,
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), document);

            // Assert
            Children(
                document,
                n => Assert.IsType<TagHelperIntermediateNode>(n));
        }

        private SourceSpan CreateSource(int number)
        {
            // The actual source span doesn't really matter, we just want to see the values used.
            return new SourceSpan(new SourceLocation(number, number, number), number);
        }
    }
}
