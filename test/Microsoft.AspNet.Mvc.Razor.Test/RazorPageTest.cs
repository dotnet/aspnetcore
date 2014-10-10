// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.PageExecutionInstrumentation;
using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorPageTest
    {
        [Fact]
        public async Task WritingScopesRedirectContentWrittenToViewContextWriter()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var page = CreatePage(v =>
            {
                v.Write("Hello Prefix");
                v.StartWritingScope();
                v.Write("Hello from Output");
                v.ViewContext.Writer.Write("Hello from view context writer");
                var scopeValue = v.EndWritingScope();
                v.Write("From Scope: " + scopeValue.ToString());
            });

            // Act
            await page.ExecuteAsync();
            var pageOutput = page.Output.ToString();

            // Assert
            Assert.Equal("Hello PrefixFrom Scope: Hello from OutputHello from view context writer", pageOutput);
        }

        [Fact]
        public async Task WritingScopesRedirectsContentWrittenToOutput()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var page = CreatePage(v =>
            {
                v.Write("Hello Prefix");
                v.StartWritingScope();
                v.Write("Hello In Scope");
                var scopeValue = v.EndWritingScope();
                v.Write("From Scope: " + scopeValue.ToString());
            });

            // Act
            await page.ExecuteAsync();
            var pageOutput = page.Output.ToString();

            // Assert
            Assert.Equal("Hello PrefixFrom Scope: Hello In Scope", pageOutput);
        }

        [Fact]
        public async Task WritingScopesCanNest()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var page = CreatePage(v =>
            {
                v.Write("Hello Prefix");
                v.StartWritingScope();
                v.Write("Hello In Scope Pre Nest");

                v.StartWritingScope();
                v.Write("Hello In Nested Scope");
                var scopeValue1 = v.EndWritingScope();

                v.Write("Hello In Scope Post Nest");
                var scopeValue2 = v.EndWritingScope();

                v.Write("From Scopes: " + scopeValue2.ToString() + scopeValue1.ToString());
            });

            // Act
            await page.ExecuteAsync();
            var pageOutput = page.Output.ToString();

            // Assert
            Assert.Equal("Hello PrefixFrom Scopes: Hello In Scope Pre NestHello In Scope Post NestHello In Nested Scope", pageOutput);
        }

        [Fact]
        public async Task StartNewWritingScope_CannotFlushInWritingScope()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var page = CreatePage(v =>
            {
                v.StartWritingScope();
                v.FlushAsync();
            });

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                                () => page.ExecuteAsync());

            // Assert
            Assert.Equal("You cannot flush while inside a writing scope.", ex.Message);
        }

        [Fact]
        public async Task StartNewWritingScope_CannotEndWritingScopeWhenNoWritingScope()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var page = CreatePage(v =>
            {
                v.EndWritingScope();
            });

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                                () => page.ExecuteAsync());

            // Assert
            Assert.Equal("There is no active writing scope to end.", ex.Message);
        }

        [Fact]
        public async Task DefineSection_ThrowsIfSectionIsAlreadyDefined()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var page = CreatePage(v =>
            {
                v.DefineSection("qux", new HelperResult(action: null));
                v.DefineSection("qux", new HelperResult(action: null));
            });

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                                () => page.ExecuteAsync());

            // Assert
            Assert.Equal("Section 'qux' is already defined.", ex.Message);
        }

        [Fact]
        public async Task RenderSection_RendersSectionFromPreviousPage()
        {
            // Arrange
            var expected = new HelperResult(action: null);
            var viewContext = CreateViewContext();
            HelperResult actual = null;
            var page = CreatePage(v =>
            {
                actual = v.RenderSection("bar");
            });
            page.PreviousSectionWriters = new Dictionary<string, HelperResult>
            {
                { "bar", expected }
            };

            // Act
            await page.ExecuteAsync();

            // Assert
            Assert.Same(actual, expected);
        }

        [Fact]
        public async Task RenderSection_ThrowsIfPreviousSectionWritersIsNotSet()
        {
            // Arrange
            Exception ex = null;
            var page = CreatePage(v =>
            {
                ex = Assert.Throws<InvalidOperationException>(() => v.RenderSection("bar"));
            });

            // Act
            await page.ExecuteAsync();

            // Assert
            Assert.Equal("The method 'RenderSection' cannot be invoked by this view.",
                         ex.Message);
        }

        [Fact]
        public async Task RenderSection_ThrowsIfRequiredSectionIsNotFound()
        {
            // Arrange
            var expected = new HelperResult(action: null);
            var page = CreatePage(v =>
            {
                v.RenderSection("bar");
            });
            page.PreviousSectionWriters = new Dictionary<string, HelperResult>
            {
                { "baz", expected }
            };

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => page.ExecuteAsync());

            // Assert
            Assert.Equal("Section 'bar' is not defined.", ex.Message);
        }

        [Fact]
        public void IsSectionDefined_ThrowsIfPreviousSectionWritersIsNotRegistered()
        {
            // Arrange
            var page = CreatePage(v => { });

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => page.IsSectionDefined("foo"),
                "The method 'IsSectionDefined' cannot be invoked by this view.");
        }

        [Fact]
        public async Task IsSectionDefined_ReturnsFalseIfSectionNotDefined()
        {
            // Arrange
            bool? actual = null;
            var page = CreatePage(v =>
            {
                actual = v.IsSectionDefined("foo");
                v.RenderSection("baz");
                v.RenderBodyPublic();
            });
            page.PreviousSectionWriters = new Dictionary<string, HelperResult>
            {
                { "baz", new HelperResult(writer => { }) }
            };
            page.RenderBodyDelegate = CreateBodyAction("body-content");

            // Act
            await page.ExecuteAsync();

            // Assert
            Assert.Equal(false, actual);
        }

        [Fact]
        public async Task IsSectionDefined_ReturnsTrueIfSectionDefined()
        {
            // Arrange
            bool? actual = null;
            var page = CreatePage(v =>
            {
                actual = v.IsSectionDefined("baz");
                v.RenderSection("baz");
                v.RenderBodyPublic();
            });
            page.PreviousSectionWriters = new Dictionary<string, HelperResult>
            {
                { "baz", new HelperResult(writer => { }) }
            };
            page.RenderBodyDelegate = CreateBodyAction("body-content");

            // Act
            await page.ExecuteAsync();

            // Assert
            Assert.Equal(true, actual);
        }

        [Fact]
        public async Task RenderSection_ThrowsIfSectionIsRenderedMoreThanOnce()
        {
            // Arrange
            var expected = new HelperResult(action: null);
            var page = CreatePage(v =>
            {
                v.RenderSection("header");
                v.RenderSection("header");
            });
            page.PreviousSectionWriters = new Dictionary<string, HelperResult>
            {
                { "header", new HelperResult(writer => { }) }
            };

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(page.ExecuteAsync);

            // Assert
            Assert.Equal("RenderSection has already been called for the section named 'header'.", ex.Message);
        }

        [Fact]
        public async Task EnsureBodyAndSectionsWereRendered_ThrowsIfDefinedSectionIsNotRendered()
        {
            // Arrange
            var expected = new HelperResult(action: null);
            var page = CreatePage(v =>
            {
                v.RenderSection("sectionA");
            });
            page.PreviousSectionWriters = new Dictionary<string, HelperResult>
            {
                { "header", expected },
                { "footer", expected },
                { "sectionA", expected },
            };

            // Act
            await page.ExecuteAsync();
            var ex = Assert.Throws<InvalidOperationException>(() => page.EnsureBodyAndSectionsWereRendered());

            // Assert
            Assert.Equal("The following sections have been defined but have not been rendered: 'header, footer'.",
                         ex.Message);
        }

        [Fact]
        public async Task EnsureBodyAndSectionsWereRendered_ThrowsIfRenderBodyIsNotCalledFromPage()
        {
            // Arrange
            var expected = new HelperResult(action: null);
            var page = CreatePage(v =>
            {
            });
            page.RenderBodyDelegate = CreateBodyAction("some content");

            // Act
            await page.ExecuteAsync();
            var ex = Assert.Throws<InvalidOperationException>(() => page.EnsureBodyAndSectionsWereRendered());

            // Assert
            Assert.Equal("RenderBody must be called from a layout page.", ex.Message);
        }

        [Fact]
        public async Task ExecuteAsync_RendersSectionsAndBody()
        {
            // Arrange
            var expected = @"Layout start
Header section
body content
Footer section
Layout end
";
            var page = CreatePage(v =>
            {
                v.WriteLiteral("Layout start" + Environment.NewLine);
                v.Write(v.RenderSection("header"));
                v.Write(v.RenderBodyPublic());
                v.Write(v.RenderSection("footer"));
                v.WriteLiteral("Layout end" + Environment.NewLine);

            });
            page.RenderBodyDelegate = CreateBodyAction("body content" + Environment.NewLine);
            page.PreviousSectionWriters = new Dictionary<string, HelperResult>
            {
                {
                    "footer", new HelperResult(writer =>
                    {
                        writer.WriteLine("Footer section");
                    })
                },
                {
                    "header", new HelperResult(writer =>
                    {
                        writer.WriteLine("Header section");
                    })
                },
            };

            // Act
            await page.ExecuteAsync();

            // Assert
            var actual = ((StringWriter)page.Output).ToString();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task Href_ReadsUrlHelperFromServiceCollection()
        {
            // Arrange
            var expected = "urlhelper-url";
            var helper = new Mock<IUrlHelper>();
            helper.Setup(h => h.Content("url"))
                  .Returns(expected)
                  .Verifiable();
            var page = CreatePage(v =>
            {
                v.Write(v.Href("url"));
            });
            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(typeof(IUrlHelper)))
                     .Returns(helper.Object);
            page.Context.RequestServices = services.Object;

            // Act
            await page.ExecuteAsync();

            // Assert
            var actual = ((StringWriter)page.Output).ToString();
            Assert.Equal(expected, actual);
            helper.Verify();
        }

        [Fact]
        public async Task FlushAsync_InvokesFlushOnWriter()
        {
            // Arrange
            var writer = new Mock<TextWriter>();
            var context = CreateViewContext(writer.Object);
            var page = CreatePage(p =>
            {
                p.FlushAsync().Wait();
            }, context);

            // Act
            await page.ExecuteAsync();

            // Assert
            writer.Verify(v => v.FlushAsync(), Times.Once());
        }

        [Fact]
        public async Task FlushAsync_ThrowsIfTheLayoutHasBeenSet()
        {
            // Arrange
            var expected = @"A layout page cannot be rendered after 'FlushAsync' has been invoked.";
            var writer = new Mock<TextWriter>();
            var context = CreateViewContext(writer.Object);
            var page = CreatePage(p =>
            {
                p.Layout = "foo";
                p.FlushAsync().Wait();
            }, context);

            // Act and Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => page.ExecuteAsync());
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public async Task FlushAsync_DoesNotThrowWhenIsRenderingLayoutIsSet()
        {
            // Arrange
            var writer = new Mock<TextWriter>();
            var context = CreateViewContext(writer.Object);
            var page = CreatePage(p =>
            {
                p.Layout = "bar";
                p.DefineSection("test-section", new HelperResult(_ =>
                {
                    p.FlushAsync().Wait();
                }));
            }, context);

            // Act
            await page.ExecuteAsync();
            page.IsLayoutBeingRendered = true;

            // Assert
            Assert.DoesNotThrow(() => page.SectionWriters["test-section"].WriteTo(TextWriter.Null));
        }

        [Fact]
        public async Task WriteAttribute_CallsBeginAndEndContext_OnPageExecutionListenerContext()
        {
            // Arrange
            var page = CreatePage(p =>
            {
                p.WriteAttribute("href",
                                 new PositionTagged<string>("prefix", 0),
                                 new PositionTagged<string>("suffix", 34),
                                 new AttributeValue(new PositionTagged<string>("prefix", 0),
                                                    new PositionTagged<object>("attr1-value", 8),
                                                    literal: true),
                                 new AttributeValue(new PositionTagged<string>("prefix2", 22),
                                                    new PositionTagged<object>("attr2", 29),
                                                    literal: false));
            });
            var context = new Mock<IPageExecutionContext>(MockBehavior.Strict);
            var sequence = new MockSequence();
            context.InSequence(sequence).Setup(f => f.BeginContext(0, 6, true)).Verifiable();
            context.InSequence(sequence).Setup(f => f.EndContext()).Verifiable();
            context.InSequence(sequence).Setup(f => f.BeginContext(8, 14, true)).Verifiable();
            context.InSequence(sequence).Setup(f => f.EndContext()).Verifiable();
            context.InSequence(sequence).Setup(f => f.BeginContext(22, 7, true)).Verifiable();
            context.InSequence(sequence).Setup(f => f.EndContext()).Verifiable();
            context.InSequence(sequence).Setup(f => f.BeginContext(29, 5, false)).Verifiable();
            context.InSequence(sequence).Setup(f => f.EndContext()).Verifiable();
            context.InSequence(sequence).Setup(f => f.BeginContext(34, 6, true)).Verifiable();
            context.InSequence(sequence).Setup(f => f.EndContext()).Verifiable();
            page.PageExecutionContext = context.Object;

            // Act
            await page.ExecuteAsync();

            // Assert
            context.Verify();
        }

        [Fact]
        public async Task WriteAttribute_CallsBeginAndEndContext_OnPrefixAndSuffixValues()
        {
            // Arrange
            var page = CreatePage(p =>
            {
                p.WriteAttribute("href",
                                 new PositionTagged<string>("prefix", 0),
                                 new PositionTagged<string>("tail", 7));
            });
            var context = new Mock<IPageExecutionContext>(MockBehavior.Strict);
            var sequence = new MockSequence();
            context.InSequence(sequence).Setup(f => f.BeginContext(0, 6, true)).Verifiable();
            context.InSequence(sequence).Setup(f => f.EndContext()).Verifiable();
            context.InSequence(sequence).Setup(f => f.BeginContext(7, 4, true)).Verifiable();
            context.InSequence(sequence).Setup(f => f.EndContext()).Verifiable();
            page.PageExecutionContext = context.Object;

            // Act
            await page.ExecuteAsync();

            // Assert
            context.Verify();
        }

        [Fact]
        public async Task Write_WithHtmlString_WritesValueWithoutEncoding()
        {
            // Arrange
            var writer = new RazorTextWriter(TextWriter.Null, Encoding.UTF8);
            var stringCollectionWriter = new StringCollectionTextWriter(Encoding.UTF8);
            stringCollectionWriter.Write("text1");
            stringCollectionWriter.Write("text2");

            var page = CreatePage(p =>
            {
                p.Write(new HtmlString("Hello world"));
                p.Write(new HtmlString(stringCollectionWriter));
            });
            page.ViewContext.Writer = writer;

            // Act
            await page.ExecuteAsync();

            // Assert
            var buffer = writer.BufferedWriter.Buffer;
            Assert.Equal(2, buffer.BufferEntries.Count);
            Assert.Equal("Hello world", buffer.BufferEntries[0]);
            Assert.Same(stringCollectionWriter.Buffer.BufferEntries, buffer.BufferEntries[1]);
        }

        private static TestableRazorPage CreatePage(Action<TestableRazorPage> executeAction,
                                                    ViewContext context = null)
        {
            context = context ?? CreateViewContext();
            var view = new Mock<TestableRazorPage> { CallBase = true };
            if (executeAction != null)
            {
                view.Setup(v => v.ExecuteAsync())
                    .Callback(() => executeAction(view.Object))
                    .Returns(Task.FromResult(0));
            }

            view.Object.ViewContext = context;
            return view.Object;
        }

        private static ViewContext CreateViewContext(TextWriter writer = null)
        {
            writer = writer ?? new StringWriter();
            var actionContext = new ActionContext(new DefaultHttpContext(), routeData: null, actionDescriptor: null);
            return new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                null,
                writer);
        }

        private static Action<TextWriter> CreateBodyAction(string value)
        {
            return writer => writer.Write(value);
        }

        public abstract class TestableRazorPage : RazorPage
        {
            public HelperResult RenderBodyPublic()
            {
                return base.RenderBody();
            }
        }
    }
}