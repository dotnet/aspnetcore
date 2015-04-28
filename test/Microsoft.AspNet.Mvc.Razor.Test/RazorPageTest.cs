// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.PageExecutionInstrumentation;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Testing;
using Microsoft.Framework.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorPageTest
    {
#pragma warning disable 1998
        private readonly RenderAsyncDelegate _nullRenderAsyncDelegate = async writer => { };
#pragma warning restore 1998

        [Fact]
        public async Task WritingScopesRedirectContentWrittenToViewContextWriter()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var page = CreatePage(v =>
            {
                v.HtmlEncoder = new CommonTestEncoder();
                v.Write("Hello Prefix");
                v.StartTagHelperWritingScope();
                v.Write("Hello from Output");
                v.ViewContext.Writer.Write("Hello from view context writer");
                var scopeValue = v.EndTagHelperWritingScope();
                v.Write("From Scope: " + scopeValue.ToString());
            });

            // Act
            await page.ExecuteAsync();
            var pageOutput = page.Output.ToString();

            // Assert
            Assert.Equal("HtmlEncode[[Hello Prefix]]HtmlEncode[[From Scope: HtmlEncode[[Hello from Output]]" +
                "Hello from view context writer]]", pageOutput);
        }

        [Fact]
        public async Task WritingScopesRedirectsContentWrittenToOutput()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var page = CreatePage(v =>
            {
                v.HtmlEncoder = new CommonTestEncoder();
                v.Write("Hello Prefix");
                v.StartTagHelperWritingScope();
                v.Write("Hello In Scope");
                var scopeValue = v.EndTagHelperWritingScope();
                v.Write("From Scope: " + scopeValue.ToString());
            });

            // Act
            await page.ExecuteAsync();
            var pageOutput = page.Output.ToString();

            // Assert
            Assert.Equal("HtmlEncode[[Hello Prefix]]HtmlEncode[[From Scope: HtmlEncode[[Hello In Scope]]]]", pageOutput);
        }

        [Fact]
        public async Task WritingScopesCanNest()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var page = CreatePage(v =>
            {
                v.HtmlEncoder = new CommonTestEncoder();
                v.Write("Hello Prefix");
                v.StartTagHelperWritingScope();
                v.Write("Hello In Scope Pre Nest");

                v.StartTagHelperWritingScope();
                v.Write("Hello In Nested Scope");
                var scopeValue1 = v.EndTagHelperWritingScope();

                v.Write("Hello In Scope Post Nest");
                var scopeValue2 = v.EndTagHelperWritingScope();

                v.Write("From Scopes: " + scopeValue2.ToString() + scopeValue1.ToString());
            });

            // Act
            await page.ExecuteAsync();
            var pageOutput = page.Output.ToString();

            // Assert
            Assert.Equal("HtmlEncode[[Hello Prefix]]HtmlEncode[[From Scopes: HtmlEncode[[Hello In Scope Pre Nest]]" +
                "HtmlEncode[[Hello In Scope Post Nest]]HtmlEncode[[Hello In Nested Scope]]]]", pageOutput);
        }

        [Fact]
        public async Task StartNewWritingScope_CannotFlushInWritingScope()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var page = CreatePage(async v =>
            {
                v.Path = "/Views/TestPath/Test.cshtml";
                v.StartTagHelperWritingScope();
                await v.FlushAsync();
            });

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                                () => page.ExecuteAsync());

            // Assert
            Assert.Equal("The FlushAsync operation cannot be performed while " +
                "inside a writing scope in '/Views/TestPath/Test.cshtml'.", ex.Message);
        }

        [Fact]
        public async Task StartNewWritingScope_CannotEndWritingScopeWhenNoWritingScope()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var page = CreatePage(v =>
            {
                v.EndTagHelperWritingScope();
            });

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                                () => page.ExecuteAsync());

            // Assert
            Assert.Equal("There is no active writing scope to end.", ex.Message);
        }

        [Fact]
        public async Task EndTagHelperWritingScope_ReturnsAppropriateContent()
        {
            // Arrange
            var viewContext = CreateViewContext();

            // Act
            var page = CreatePage(v =>
            {
                v.HtmlEncoder = new CommonTestEncoder();
                v.StartTagHelperWritingScope();
                v.Write("Hello World!");
                var returnValue = v.EndTagHelperWritingScope();

                // Assert
                var content = Assert.IsType<DefaultTagHelperContent>(returnValue);
                Assert.Equal("HtmlEncode[[Hello World!]]", content.GetContent());
            });
            await page.ExecuteAsync();
        }

        [Fact]
        public async Task EndTagHelperWritingScope_CopiesContent_IfRazorTextWriter()
        {
            // Arrange
            var viewContext = CreateViewContext();

            // Act
            var page = CreatePage(v =>
            {
                v.HtmlEncoder = new CommonTestEncoder();
                v.StartTagHelperWritingScope(new RazorTextWriter(TextWriter.Null, Encoding.UTF8));
                v.Write("Hello ");
                v.Write("World!");
                var returnValue = v.EndTagHelperWritingScope();

                // Assert
                var content = Assert.IsType<DefaultTagHelperContent>(returnValue);
                Assert.Equal("HtmlEncode[[Hello ]]HtmlEncode[[World!]]", content.GetContent());
                Assert.Equal(new[] { "HtmlEncode[[Hello ]]", "HtmlEncode[[World!]]" }, content.ToArray());
            }, viewContext);
            await page.ExecuteAsync();
        }

        [Fact]
        public async Task DefineSection_ThrowsIfSectionIsAlreadyDefined()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var page = CreatePage(v =>
            {
                v.DefineSection("qux", _nullRenderAsyncDelegate);
                v.DefineSection("qux", _nullRenderAsyncDelegate);
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
            var expected = "Hello world";
            var viewContext = CreateViewContext();
            var page = CreatePage(v =>
            {
                v.Write(v.RenderSection("bar"));
            });
            page.PreviousSectionWriters = new Dictionary<string, RenderAsyncDelegate>
            {
                { "bar", writer => writer.WriteAsync(expected) }
            };

            // Act
            await page.ExecuteAsync();

            // Assert
            Assert.Equal(expected, page.RenderedContent);
        }

        [Fact]
        public async Task RenderSection_ThrowsIfPreviousSectionWritersIsNotSet()
        {
            // Arrange
            Exception ex = null;
            var page = CreatePage(v =>
            {
                v.Path = "/Views/TestPath/Test.cshtml";
                ex = Assert.Throws<InvalidOperationException>(() => v.RenderSection("bar"));
            });

            // Act
            await page.ExecuteAsync();

            // Assert
            Assert.Equal("RenderSection invocation in '/Views/TestPath/Test.cshtml' is invalid. " +
                "RenderSection can only be called from a layout page.",
                ex.Message);
        }

        [Fact]
        public async Task RenderSection_ThrowsIfRequiredSectionIsNotFound()
        {
            // Arrange
            var page = CreatePage(v =>
            {
                v.Path = "/Views/TestPath/Test.cshtml";
                v.RenderSection("bar");
            });
            page.PreviousSectionWriters = new Dictionary<string, RenderAsyncDelegate>
            {
                { "baz", _nullRenderAsyncDelegate }
            };

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => page.ExecuteAsync());

            // Assert
            Assert.Equal("Section 'bar' is not defined in path '/Views/TestPath/Test.cshtml'.", ex.Message);
        }

        [Fact]
        public void IsSectionDefined_ThrowsIfPreviousSectionWritersIsNotRegistered()
        {
            // Arrange
            var page = CreatePage(v => 
            {
                v.Path = "/Views/TestPath/Test.cshtml";
            });

            // Act and Assert
            page.ExecuteAsync();
            ExceptionAssert.Throws<InvalidOperationException>(() => page.IsSectionDefined("foo"),
                "IsSectionDefined invocation in '/Views/TestPath/Test.cshtml' is invalid." +
                " IsSectionDefined can only be called from a layout page.");
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
            page.PreviousSectionWriters = new Dictionary<string, RenderAsyncDelegate>
            {
                { "baz", _nullRenderAsyncDelegate }
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
            page.PreviousSectionWriters = new Dictionary<string, RenderAsyncDelegate>
            {
                { "baz", _nullRenderAsyncDelegate }
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
                v.Path = "/Views/TestPath/Test.cshtml";
                v.RenderSection("header");
                v.RenderSection("header");
            });
            page.PreviousSectionWriters = new Dictionary<string, RenderAsyncDelegate>
            {
                { "header", _nullRenderAsyncDelegate }
            };

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(page.ExecuteAsync);

            // Assert
            Assert.Equal("RenderSectionAsync invocation in '/Views/TestPath/Test.cshtml' is invalid." +
                " The section 'header' has already been rendered.", ex.Message);
        }

        [Fact]
        public async Task RenderSectionAsync_ThrowsIfSectionIsRenderedMoreThanOnce()
        {
            // Arrange
            var expected = new HelperResult(action: null);
            var page = CreatePage(async v =>
            {
                v.Path = "/Views/TestPath/Test.cshtml";
                await v.RenderSectionAsync("header");
                await v.RenderSectionAsync("header");
            });
            page.PreviousSectionWriters = new Dictionary<string, RenderAsyncDelegate>
            {
                { "header", _nullRenderAsyncDelegate }
            };

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(page.ExecuteAsync);

            // Assert
            Assert.Equal("RenderSectionAsync invocation in '/Views/TestPath/Test.cshtml' is invalid." +
                " The section 'header' has already been rendered.", ex.Message);
        }

        [Fact]
        public async Task RenderSectionAsync_ThrowsIfSectionIsRenderedMoreThanOnce_WithSyncMethod()
        {
            // Arrange
            var expected = new HelperResult(action: null);
            var page = CreatePage(async v =>
            {
                v.Path = "/Views/TestPath/Test.cshtml";
                v.RenderSection("header");
                await v.RenderSectionAsync("header");
            });
            page.PreviousSectionWriters = new Dictionary<string, RenderAsyncDelegate>
            {
                { "header", _nullRenderAsyncDelegate }
            };

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(page.ExecuteAsync);

            // Assert
            Assert.Equal("RenderSectionAsync invocation in '/Views/TestPath/Test.cshtml' is invalid." +
                " The section 'header' has already been rendered.", ex.Message);
        }

        [Fact]
        public async Task RenderSectionAsync_ThrowsIfNotInvokedFromLayoutPage()
        {
            // Arrange
            var expected = new HelperResult(action: null);
            var page = CreatePage(async v =>
            {
                v.Path = "/Views/TestPath/Test.cshtml";
                await v.RenderSectionAsync("header");
            });

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(page.ExecuteAsync);

            // Assert
            Assert.Equal("RenderSectionAsync invocation in '/Views/TestPath/Test.cshtml' is invalid. " +
                "RenderSectionAsync can only be called from a layout page.", ex.Message);
        }

        [Fact]
        public async Task EnsureRenderedBodyOrSections_ThrowsIfRenderBodyIsNotCalledFromPage_AndNoSectionsAreDefined()
        {
            // Arrange
            var path = "page-path";
            var page = CreatePage(v =>
            {
            });
            page.Path = path;
            page.RenderBodyDelegate = CreateBodyAction("some content");

            // Act
            await page.ExecuteAsync();
            var ex = Assert.Throws<InvalidOperationException>(() => page.EnsureRenderedBodyOrSections());

            // Assert
            Assert.Equal($"RenderBody has not been called for the page at '{path}'.", ex.Message);
        }

        [Fact]
        public async Task EnsureRenderedBodyOrSections_ThrowsIfDefinedSectionsAreNotRendered()
        {
            // Arrange
            var path = "page-path";
            var sectionName = "sectionA";
            var page = CreatePage(v =>
            {
            });
            page.Path = path;
            page.RenderBodyDelegate = CreateBodyAction("some content");
            page.PreviousSectionWriters = new Dictionary<string, RenderAsyncDelegate>
            {
                { sectionName, _nullRenderAsyncDelegate }
            };

            // Act
            await page.ExecuteAsync();
            var ex = Assert.Throws<InvalidOperationException>(() => page.EnsureRenderedBodyOrSections());

            // Assert
            Assert.Equal("The following sections have been defined but have not been rendered by the page at " +
                $"'{path}': '{sectionName}'.", ex.Message);
        }

        [Fact]
        public async Task EnsureRenderedBodyOrSections_SucceedsIfRenderBodyIsNotCalled_ButAllDefinedSectionsAreRendered()
        {
            // Arrange
            var sectionA = "sectionA";
            var sectionB = "sectionB";
            var page = CreatePage(v =>
            {
                v.RenderSection(sectionA);
                v.RenderSection(sectionB);
            });
            page.RenderBodyDelegate = CreateBodyAction("some content");
            page.PreviousSectionWriters = new Dictionary<string, RenderAsyncDelegate>
            {
                { sectionA, _nullRenderAsyncDelegate },
                { sectionB, _nullRenderAsyncDelegate },
            };

            // Act and Assert
            await page.ExecuteAsync();
            page.EnsureRenderedBodyOrSections();
        }

        [Fact]
        public async Task ExecuteAsync_RendersSectionsAndBody()
        {
            // Arrange
            var expected = string.Join(Environment.NewLine,
                                       "Layout start",
                                       "Header section",
                                       "Async Header section",
                                       "body content",
                                       "Async Footer section",
                                       "Footer section",
                                       "Layout end");
            var page = CreatePage(async v =>
            {
                v.WriteLiteral("Layout start" + Environment.NewLine);
                v.Write(v.RenderSection("header"));
                v.Write(await v.RenderSectionAsync("async-header"));
                v.Write(v.RenderBodyPublic());
                v.Write(await v.RenderSectionAsync("async-footer"));
                v.Write(v.RenderSection("footer"));
                v.WriteLiteral("Layout end");
            });
            page.RenderBodyDelegate = CreateBodyAction("body content" + Environment.NewLine);
            page.PreviousSectionWriters = new Dictionary<string, RenderAsyncDelegate>
            {
                {
                    "footer", writer => writer.WriteLineAsync("Footer section")
                },
                {
                    "header", writer => writer.WriteLineAsync("Header section")
                },
                {
                    "async-header", writer => writer.WriteLineAsync("Async Header section")
                },
                {
                    "async-footer", writer => writer.WriteLineAsync("Async Footer section")
                },
            };

            // Act
            await page.ExecuteAsync();

            // Assert
            var actual = page.RenderedContent;
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
                v.HtmlEncoder = new CommonTestEncoder();
                v.Write(v.Href("url"));
            });
            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(typeof(IUrlHelper)))
                     .Returns(helper.Object);
            page.Context.RequestServices = services.Object;

            // Act
            await page.ExecuteAsync();

            // Assert
            var actual = page.RenderedContent;
            Assert.Equal($"HtmlEncode[[{expected}]]", actual);
            helper.Verify();
        }

        [Fact]
        public async Task FlushAsync_InvokesFlushOnWriter()
        {
            // Arrange
            var writer = new Mock<TextWriter>();
            var context = CreateViewContext(writer.Object);
            var page = CreatePage(async p =>
            {
                await p.FlushAsync();
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
            var expected = "Layout page '/Views/TestPath/Test.cshtml' cannot be rendered" +
                " after 'FlushAsync' has been invoked.";
            var writer = new Mock<TextWriter>();
            var context = CreateViewContext(writer.Object);
            var page = CreatePage(async p =>
            {
                p.Path = "/Views/TestPath/Test.cshtml";
                p.Layout = "foo";
                await p.FlushAsync();
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
                p.DefineSection("test-section", async _ =>
                {
                    await p.FlushAsync();
                });
            }, context);

            // Act
            await page.ExecuteAsync();
            page.IsLayoutBeingRendered = true;

            // Assert (does not throw)
            var renderAsyncDelegate = page.SectionWriters["test-section"];
            await renderAsyncDelegate(TextWriter.Null);
        }

        [Fact]
        public async Task FlushAsync_ReturnsEmptyHtmlString()
        {
            // Arrange
            HtmlString actual = null;
            var writer = new Mock<TextWriter>();
            var context = CreateViewContext(writer.Object);
            var page = CreatePage(async p =>
            {
                actual = await p.FlushAsync();
            }, context);

            // Act
            await page.ExecuteAsync();

            // Assert
            Assert.Same(HtmlString.Empty, actual);
        }

        [Fact]
        public async Task WriteAttribute_CallsBeginAndEndContext_OnPageExecutionListenerContext()
        {
            // Arrange
            var page = CreatePage(p =>
            {
                p.HtmlEncoder = new CommonTestEncoder();
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
            Assert.Equal(3, buffer.BufferEntries.Count);
            Assert.Equal("Hello world", buffer.BufferEntries[0]);
            Assert.Equal("text1", buffer.BufferEntries[1]);
            Assert.Equal("text2", buffer.BufferEntries[2]);
        }

        public static TheoryData<TagHelperOutput, string> WriteTagHelper_InputData
        {
            get
            {
                // parameters: TagHelperOutput, expectedOutput
                return new TheoryData<TagHelperOutput, string>
                {
                    {
                        // parameters: TagName, Attributes, SelfClosing, PreContent, Content, PostContent
                        GetTagHelperOutput(
                            tagName:     "div",
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: false,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<div>Hello World!</div>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     null,
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: false,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "Hello World!"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "  ",
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: false,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "Hello World!"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList() { { "test", "testVal" } },
                            selfClosing: false,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p test=\"HtmlEncode[[testVal]]\">Hello World!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList() { { "test", "testVal" }, { "something", "  spaced  " } },
                            selfClosing: false,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p test=\"HtmlEncode[[testVal]]\" something=\"HtmlEncode[[  spaced  ]]\">Hello World!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList() { { "test", "testVal" } },
                            selfClosing: true,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p test=\"HtmlEncode[[testVal]]\" />"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList() { { "test", "testVal" }, { "something", "  spaced  " } },
                            selfClosing: true,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p test=\"HtmlEncode[[testVal]]\" something=\"HtmlEncode[[  spaced  ]]\" />"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: false,
                            preElement:  null,
                            preContent:  "Hello World!",
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "<p>Hello World!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: false,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p>Hello World!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: false,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: "Hello World!",
                            postElement: null),
                        "<p>Hello World!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: false,
                            preElement:  null,
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: null),
                        "<p>HelloTestWorld!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: true,
                            preElement:  null,
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: null),
                        "<p />"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: false,
                            preElement:  null,
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: null),
                        "<custom>HelloTestWorld!</custom>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "random",
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: true,
                            preElement:  null,
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: null),
                        "<random />"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: false,
                            preElement:  "Before",
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "Before<custom></custom>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     null,
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: false,
                            preElement:  "Before",
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "Before"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     null,
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            selfClosing: true,
                            preElement:  "Before",
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "Before"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            selfClosing: true,
                            preElement:  "Before",
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "Before<custom test=\"HtmlEncode[[testVal]]\" />"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: true,
                            preElement:  "Before",
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "Before<custom />"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: false,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: "After"),
                        "<custom></custom>After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     null,
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: false,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: "After"),
                        "After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     null,
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            selfClosing: true,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: "After"),
                        "After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            selfClosing: true,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: "After"),
                        "<custom test=\"HtmlEncode[[testVal]]\" />After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: true,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: "After"),
                        "<custom />After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: false,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "Before<custom>HelloTestWorld!</custom>After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            selfClosing: false,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "Before<custom test=\"HtmlEncode[[testVal]]\">HelloTestWorld!</custom>After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: true,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "Before<custom />After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     null,
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: true,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "BeforeHelloTestWorld!After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     null,
                            attributes:  new TagHelperAttributeList(),
                            selfClosing: false,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "BeforeHelloTestWorld!After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     null,
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            selfClosing: false,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "BeforeHelloTestWorld!After"
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(WriteTagHelper_InputData))]
        public async Task WriteTagHelperAsync_WritesFormattedTagHelper(TagHelperOutput output, string expected)
        {
            // Arrange
            var writer = new StringCollectionTextWriter(Encoding.UTF8);
            var context = CreateViewContext(writer);
            var tagHelperExecutionContext = new TagHelperExecutionContext(
                tagName: output.TagName,
                selfClosing: output.SelfClosing,
                items: new Dictionary<object, object>(),
                uniqueId: string.Empty,
                executeChildContentAsync: () => Task.FromResult(result: true),
                startTagHelperWritingScope: () => { },
                endTagHelperWritingScope: () => new DefaultTagHelperContent());
            tagHelperExecutionContext.Output = output;

            // Act
            var page = CreatePage(p =>
            {
                p.HtmlEncoder = new CommonTestEncoder();
                p.WriteTagHelperAsync(tagHelperExecutionContext).Wait();
            }, context);
            await page.ExecuteAsync();

            // Assert
            Assert.Equal(expected, writer.ToString());
        }

        [Theory]
        // This is a scenario where GetChildContentAsync is called.
        [InlineData(true, "HelloWorld!", "<p>HelloWorld!</p>")]
        // This is a scenario where ExecuteChildContentAsync is called.
        [InlineData(false, "HelloWorld!", "<p></p>")]
        public async Task WriteTagHelperAsync_WritesContentAppropriately(
            bool childContentRetrieved, string input, string expected)
        {
            // Arrange
            var defaultTagHelperContent = new DefaultTagHelperContent();
            var writer = new StringCollectionTextWriter(Encoding.UTF8);
            var context = CreateViewContext(writer);
            var tagHelperExecutionContext = new TagHelperExecutionContext(
                tagName: "p",
                selfClosing: false,
                items: new Dictionary<object, object>(),
                uniqueId: string.Empty,
                executeChildContentAsync: () =>
                {
                    defaultTagHelperContent.SetContent(input);
                    return Task.FromResult(result: true);
                },
                startTagHelperWritingScope: () => { },
                endTagHelperWritingScope: () => defaultTagHelperContent);
            tagHelperExecutionContext.Output = new TagHelperOutput("p", new TagHelperAttributeList());
            if (childContentRetrieved)
            {
                await tagHelperExecutionContext.GetChildContentAsync();
            }

            // Act
            var page = CreatePage(p =>
            {
                p.HtmlEncoder = new CommonTestEncoder();
                p.WriteTagHelperAsync(tagHelperExecutionContext).Wait();
            }, context);
            await page.ExecuteAsync();

            // Assert
            Assert.Equal(expected, writer.ToString());
        }

        [Fact]
        public async Task WriteTagHelperToAsync_WritesToSpecifiedWriter()
        {
            // Arrange
            var writer = new StringCollectionTextWriter(Encoding.UTF8);
            var context = CreateViewContext(new StringWriter());
            var tagHelperExecutionContext = new TagHelperExecutionContext(
                tagName: "p",
                selfClosing: false,
                items: new Dictionary<object, object>(),
                uniqueId: string.Empty,
                executeChildContentAsync: () => { return Task.FromResult(result: true); },
                startTagHelperWritingScope: () => { },
                endTagHelperWritingScope: () => new DefaultTagHelperContent());
            tagHelperExecutionContext.Output = new TagHelperOutput("p", new TagHelperAttributeList());
            tagHelperExecutionContext.Output.Content.SetContent("Hello World!");

            // Act
            var page = CreatePage(p =>
            {
                p.WriteTagHelperToAsync(writer, tagHelperExecutionContext).Wait();
            }, context);
            await page.ExecuteAsync();

            // Assert
            Assert.Equal("<p>Hello World!</p>", writer.ToString());
        }

        [Theory]
        [MemberData(nameof(WriteTagHelper_InputData))]
        public async Task WriteTagHelperToAsync_WritesFormattedTagHelper(TagHelperOutput output, string expected)
        {
            // Arrange
            var writer = new StringCollectionTextWriter(Encoding.UTF8);
            var context = CreateViewContext(new StringWriter());
            var tagHelperExecutionContext = new TagHelperExecutionContext(
                tagName: output.TagName,
                selfClosing: output.SelfClosing,
                items: new Dictionary<object, object>(),
                uniqueId: string.Empty,
                executeChildContentAsync: () => Task.FromResult(result: true),
                startTagHelperWritingScope: () => { },
                endTagHelperWritingScope: () => new DefaultTagHelperContent());
            tagHelperExecutionContext.Output = output;

            // Act
            var page = CreatePage(p =>
            {
                p.HtmlEncoder = new CommonTestEncoder();
                p.WriteTagHelperToAsync(writer, tagHelperExecutionContext).Wait();
            }, context);
            await page.ExecuteAsync();

            // Assert
            Assert.Equal(expected, writer.ToString());
        }

        private static TagHelperOutput GetTagHelperOutput(
            string tagName,
            TagHelperAttributeList attributes,
            bool selfClosing,
            string preElement,
            string preContent,
            string content,
            string postContent,
            string postElement)
        {
            var output = new TagHelperOutput(tagName, attributes)
            {
                SelfClosing = selfClosing
            };

            output.PreElement.SetContent(preElement);
            output.PreContent.SetContent(preContent);
            output.Content.SetContent(content);
            output.PostContent.SetContent(postContent);
            output.PostElement.SetContent(postElement);

            return output;
        }

        private static TestableRazorPage CreatePage(Action<TestableRazorPage> executeAction,
                                                    ViewContext context = null)
        {
            return CreatePage(page =>
            {
                executeAction(page);
                return Task.FromResult(0);
            }, context);
        }


        private static TestableRazorPage CreatePage(Func<TestableRazorPage, Task> executeAction,
                                                    ViewContext context = null)
        {
            context = context ?? CreateViewContext();
            var view = new Mock<TestableRazorPage> { CallBase = true };
            if (executeAction != null)
            {
                view.Setup(v => v.ExecuteAsync())
                    .Returns(() =>
                    {
                        return executeAction(view.Object);
                    });
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
                Mock.Of<ITempDataDictionary>(),
                writer,
                new HtmlHelperOptions());
        }

        private static Action<TextWriter> CreateBodyAction(string value)
        {
            return writer => writer.Write(value);
        }

        public abstract class TestableRazorPage : RazorPage
        {
            public string RenderedContent
            {
                get
                {
                    var writer = Assert.IsType<StringWriter>(Output);
                    return writer.ToString();
                }
            }

            public HelperResult RenderBodyPublic()
            {
                return base.RenderBody();
            }
        }
    }
}