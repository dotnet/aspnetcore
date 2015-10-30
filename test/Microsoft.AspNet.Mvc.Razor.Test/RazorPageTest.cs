// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Html.Abstractions;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.TestCommon;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.PageExecutionInstrumentation;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Testing;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorPageTest
    {
        private readonly RenderAsyncDelegate _nullRenderAsyncDelegate = writer => Task.FromResult(0);
        private readonly Func<TextWriter, Task> NullAsyncWrite = CreateAsyncWriteDelegate(string.Empty);

        [Fact]
        public async Task WritingScopesRedirectContentWrittenToViewContextWriter()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var page = CreatePage(v =>
            {
                v.HtmlEncoder = new HtmlTestEncoder();
                v.Write("Hello Prefix");
                v.StartTagHelperWritingScope();
                v.Write("Hello from Output");
                v.ViewContext.Writer.Write("Hello from view context writer");
                var scopeValue = v.EndTagHelperWritingScope();
                v.Write("From Scope: ");
                v.Write(scopeValue);
            });

            // Act
            await page.ExecuteAsync();
            var pageOutput = page.Output.ToString();

            // Assert
            Assert.Equal("HtmlEncode[[Hello Prefix]]HtmlEncode[[From Scope: ]]HtmlEncode[[Hello from Output]]" +
                "Hello from view context writer", pageOutput);
        }

        [Fact]
        public async Task WritingScopesRedirectsContentWrittenToOutput()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var page = CreatePage(v =>
            {
                v.HtmlEncoder = new HtmlTestEncoder();
                v.Write("Hello Prefix");
                v.StartTagHelperWritingScope();
                v.Write("Hello In Scope");
                var scopeValue = v.EndTagHelperWritingScope();
                v.Write("From Scope: ");
                v.Write(scopeValue);
            });

            // Act
            await page.ExecuteAsync();
            var pageOutput = page.Output.ToString();

            // Assert
            Assert.Equal("HtmlEncode[[Hello Prefix]]HtmlEncode[[From Scope: ]]HtmlEncode[[Hello In Scope]]", pageOutput);
        }

        [Fact]
        public async Task WritingScopesCanNest()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var page = CreatePage(v =>
            {
                v.HtmlEncoder = new HtmlTestEncoder();
                v.Write("Hello Prefix");
                v.StartTagHelperWritingScope();
                v.Write("Hello In Scope Pre Nest");

                v.StartTagHelperWritingScope();
                v.Write("Hello In Nested Scope");
                var scopeValue1 = v.EndTagHelperWritingScope();

                v.Write("Hello In Scope Post Nest");
                var scopeValue2 = v.EndTagHelperWritingScope();

                v.Write("From Scopes: ");
                v.Write(scopeValue2);
                v.Write(scopeValue1);
            });

            // Act
            await page.ExecuteAsync();
            var pageOutput = page.Output.ToString();

            // Assert
            Assert.Equal("HtmlEncode[[Hello Prefix]]HtmlEncode[[From Scopes: ]]HtmlEncode[[Hello In Scope Pre Nest]]" +
                "HtmlEncode[[Hello In Scope Post Nest]]HtmlEncode[[Hello In Nested Scope]]", pageOutput);
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
                v.HtmlEncoder = new HtmlTestEncoder();
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
                v.HtmlEncoder = new HtmlTestEncoder();
                v.StartTagHelperWritingScope(new RazorTextWriter(TextWriter.Null, Encoding.UTF8, v.HtmlEncoder));
                v.Write("Hello ");
                v.Write("World!");
                var returnValue = v.EndTagHelperWritingScope();

                // Assert
                var content = Assert.IsType<DefaultTagHelperContent>(returnValue);
                Assert.Equal("HtmlEncode[[Hello ]]HtmlEncode[[World!]]", content.GetContent());
                Assert.Equal(
                    "HtmlEncode[[Hello ]]HtmlEncode[[World!]]",
                    HtmlContentUtilities.HtmlContentToString(content));
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
            page.RenderBodyDelegateAsync = CreateAsyncWriteDelegate("body-content");

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
            page.RenderBodyDelegateAsync = CreateAsyncWriteDelegate("body-content");

            // Act
            await page.ExecuteAsync();

            // Assert
            Assert.Equal(true, actual);
        }

        [Fact]
        public async Task RenderSection_ThrowsIfSectionIsRenderedMoreThanOnce()
        {
            // Arrange
            var expected = new HelperResult(NullAsyncWrite);
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
            var expected = new HelperResult(NullAsyncWrite);
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
            var expected = new HelperResult(NullAsyncWrite);
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
            var expected = new HelperResult(NullAsyncWrite);
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
            page.RenderBodyDelegateAsync = CreateAsyncWriteDelegate("some content");

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
            page.RenderBodyDelegateAsync = CreateAsyncWriteDelegate("some content");
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
            page.RenderBodyDelegateAsync = CreateAsyncWriteDelegate("some content");
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
            page.RenderBodyDelegateAsync = CreateAsyncWriteDelegate("body content" + Environment.NewLine);
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
                v.HtmlEncoder = new HtmlTestEncoder();
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
                p.HtmlEncoder = new HtmlTestEncoder();
                p.BeginWriteAttribute("href", "prefix", 0, "suffix", 34, 2);
                p.WriteAttributeValue("prefix", 0, "attr1-value", 8, 14, true);
                p.WriteAttributeValue("prefix2", 22, "attr2", 29, 5, false);
                p.EndWriteAttribute();
            });
            var context = new Mock<IPageExecutionContext>(MockBehavior.Strict);
            var sequence = new MockSequence();
            context.InSequence(sequence).Setup(f => f.BeginContext(0, 6, true)).Verifiable();
            context.InSequence(sequence).Setup(f => f.EndContext()).Verifiable();
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
        public async Task WriteAttribute_WithBoolValue_CallsBeginAndEndContext_OnPageExecutionListenerContext()
        {
            // Arrange
            var page = CreatePage(p =>
            {
                p.HtmlEncoder = new HtmlTestEncoder();
                p.BeginWriteAttribute("href", "prefix", 0, "suffix", 10, 1);
                p.WriteAttributeValue("", 6, "true", 6, 4, false);
                p.EndWriteAttribute();
            });
            var context = new Mock<IPageExecutionContext>(MockBehavior.Strict);
            var sequence = new MockSequence();
            context.InSequence(sequence).Setup(f => f.BeginContext(0, 6, true)).Verifiable();
            context.InSequence(sequence).Setup(f => f.EndContext()).Verifiable();
            context.InSequence(sequence).Setup(f => f.BeginContext(6, 4, false)).Verifiable();
            context.InSequence(sequence).Setup(f => f.EndContext()).Verifiable();
            context.InSequence(sequence).Setup(f => f.BeginContext(10, 6, true)).Verifiable();
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
                p.BeginWriteAttribute("href", "prefix", 0, "tail", 7, 0);
                p.EndWriteAttribute();
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

        public static TheoryData AddHtmlAttributeValues_ValueData
        {
            get
            {
                // attributeValues, expectedValue
                return new TheoryData<Tuple<string, int, object, int, bool>[], string>
                {
                    {
                        new []
                        {
                            Tuple.Create(string.Empty, 9, (object)"Hello", 9, true),
                        },
                        "Hello"
                    },
                    {
                        new []
                        {
                            Tuple.Create(" ", 9, (object)"Hello", 10, true)
                        },
                        " Hello"
                    },
                    {

                        new []
                        {
                            Tuple.Create(" ", 9, (object)null, 10, false)
                        },
                        string.Empty
                    },
                    {
                        new []
                        {
                            Tuple.Create(" ", 9, (object)false, 10, false)
                        },
                        " HtmlEncode[[False]]"
                    },
                    {
                        new []
                        {
                            Tuple.Create("  ", 9, (object)true, 11, false),
                            Tuple.Create("  ", 9, (object)"abcd", 17, true)
                        },
                        "  HtmlEncode[[True]]  abcd"
                    },
                    {
                        new []
                        {
                            Tuple.Create(string.Empty, 9, (object)"prefix", 9, true),
                            Tuple.Create("  ", 15, (object)null, 17, false),
                            Tuple.Create(" ", 21, (object)"suffix", 22, false),
                        },
                        "prefix HtmlEncode[[suffix]]"
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(AddHtmlAttributeValues_ValueData))]
        public void AddHtmlAttributeValues_AddsToHtmlAttributesAsExpected(
            Tuple<string, int, object, int, bool>[] attributeValues,
            string expectedValue)
        {
            // Arrange
            var page = CreatePage(p => { });
            page.HtmlEncoder = new HtmlTestEncoder();
            var executionContext = new TagHelperExecutionContext(
                "p",
                tagMode: TagMode.StartTagAndEndTag,
                items: new Dictionary<object, object>(),
                uniqueId: string.Empty,
                executeChildContentAsync: () => Task.FromResult(result: true),
                startTagHelperWritingScope: () => { },
                endTagHelperWritingScope: () => new DefaultTagHelperContent());

            // Act
            page.BeginAddHtmlAttributeValues(executionContext, "someattr", attributeValues.Length);
            foreach (var value in attributeValues)
            {
                page.AddHtmlAttributeValue(value.Item1, value.Item2, value.Item3, value.Item4, 0, value.Item5);
            }
            page.EndAddHtmlAttributeValues(executionContext);

            // Assert
            var htmlAttribute = Assert.Single(executionContext.HTMLAttributes);
            Assert.Equal("someattr", htmlAttribute.Name, StringComparer.Ordinal);
            Assert.IsType<HtmlString>(htmlAttribute.Value);
            Assert.Equal(expectedValue, HtmlContentUtilities.HtmlContentToString((IHtmlContent)htmlAttribute.Value));
            Assert.False(htmlAttribute.Minimized);
            var allAttribute = Assert.Single(executionContext.AllAttributes);
            Assert.Equal("someattr", allAttribute.Name, StringComparer.Ordinal);
            Assert.IsType<HtmlString>(allAttribute.Value);
            Assert.Equal(expectedValue, allAttribute.Value.ToString(), StringComparer.Ordinal);
            Assert.False(allAttribute.Minimized);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData(false, "False")]
        public void AddHtmlAttributeValues_OnlyAddsToAllAttributesWhenAttributeRemoved(
            object attributeValue,
            string expectedValue)
        {
            // Arrange
            var page = CreatePage(p => { });
            page.HtmlEncoder = new HtmlTestEncoder();
            var executionContext = new TagHelperExecutionContext(
                "p",
                tagMode: TagMode.StartTagAndEndTag,
                items: new Dictionary<object, object>(),
                uniqueId: string.Empty,
                executeChildContentAsync: () => Task.FromResult(result: true),
                startTagHelperWritingScope: () => { },
                endTagHelperWritingScope: () => new DefaultTagHelperContent());

            // Act
            page.BeginAddHtmlAttributeValues(executionContext, "someattr", 1);
            page.AddHtmlAttributeValue(string.Empty, 9, attributeValue, 9, valueLength: 0, isLiteral: false);
            page.EndAddHtmlAttributeValues(executionContext);

            // Assert
            Assert.Empty(executionContext.HTMLAttributes);
            var attribute = Assert.Single(executionContext.AllAttributes);
            Assert.Equal("someattr", attribute.Name, StringComparer.Ordinal);
            Assert.Equal(expectedValue, (string)attribute.Value, StringComparer.Ordinal);
            Assert.False(attribute.Minimized);
        }

        [Fact]
        public void AddHtmlAttributeValues_AddsAttributeNameAsValueWhenValueIsUnprefixedTrue()
        {
            // Arrange
            var page = CreatePage(p => { });
            page.HtmlEncoder = new HtmlTestEncoder();
            var executionContext = new TagHelperExecutionContext(
                "p",
                tagMode: TagMode.StartTagAndEndTag,
                items: new Dictionary<object, object>(),
                uniqueId: string.Empty,
                executeChildContentAsync: () => Task.FromResult(result: true),
                startTagHelperWritingScope: () => { },
                endTagHelperWritingScope: () => new DefaultTagHelperContent());

            // Act
            page.BeginAddHtmlAttributeValues(executionContext, "someattr", 1);
            page.AddHtmlAttributeValue(string.Empty, 9, true, 9, valueLength: 0, isLiteral: false);
            page.EndAddHtmlAttributeValues(executionContext);

            // Assert
            var htmlAttribute = Assert.Single(executionContext.HTMLAttributes);
            Assert.Equal("someattr", htmlAttribute.Name, StringComparer.Ordinal);
            Assert.Equal("someattr", (string)htmlAttribute.Value, StringComparer.Ordinal);
            Assert.False(htmlAttribute.Minimized);
            var allAttribute = Assert.Single(executionContext.AllAttributes);
            Assert.Equal("someattr", allAttribute.Name, StringComparer.Ordinal);
            Assert.Equal("someattr", (string)allAttribute.Value, StringComparer.Ordinal);
            Assert.False(allAttribute.Minimized);
        }

        public static TheoryData WriteAttributeData
        {
            get
            {
                // AttributeValues, ExpectedOutput
                return new TheoryData<Tuple<string, int, object, int, bool>[], string>
                {
                    {
                        new[]
                        {
                            Tuple.Create(string.Empty, 9, (object)true, 9, false),
                        },
                        "someattr=HtmlEncode[[someattr]]"
                    },
                    {
                        new[]
                        {
                            Tuple.Create(string.Empty, 9, (object)false, 9, false),
                        },
                        string.Empty
                    },
                    {
                        new[]
                        {
                            Tuple.Create(string.Empty, 9, (object)null, 9, false),
                        },
                        string.Empty
                    },
                    {
                        new[]
                        {
                            Tuple.Create("  ", 9, (object)false, 11, false),
                        },
                        "someattr=  HtmlEncode[[False]]"
                    },
                    {
                        new[]
                        {
                            Tuple.Create("  ", 9, (object)null, 11, false),
                        },
                        "someattr="
                    },
                    {
                        new[]
                        {
                            Tuple.Create("  ", 9, (object)true, 11, false),
                            Tuple.Create("  ", 15, (object)"abcd", 17, true),
                        },
                        "someattr=  HtmlEncode[[True]]  abcd"
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(WriteAttributeData))]
        public void WriteAttributeTo_WritesAsExpected(
            Tuple<string, int, object, int, bool>[] attributeValues,
            string expectedOutput)
        {
            // Arrange
            var page = CreatePage(p => { });
            page.HtmlEncoder = new HtmlTestEncoder();
            var writer = new StringWriter();
            var prefix = "someattr=";
            var suffix = string.Empty;

            // Act
            page.BeginWriteAttributeTo(writer, "someattr", prefix, 0, suffix, 0, attributeValues.Length);
            foreach (var value in attributeValues)
            {
                page.WriteAttributeValueTo(
                    writer,
                    value.Item1,
                    value.Item2,
                    value.Item3,
                    value.Item4,
                    value.Item3?.ToString().Length ?? 0,
                    value.Item5);
            }
            page.EndWriteAttributeTo(writer);

            // Assert
            Assert.Equal(expectedOutput, writer.ToString());
        }

        [Fact]
        public async Task Write_WithHtmlString_WritesValueWithoutEncoding()
        {
            // Arrange
            var writer = new RazorTextWriter(TextWriter.Null, Encoding.UTF8, new HtmlTestEncoder());

            var page = CreatePage(p =>
            {
                p.Write(new HtmlString("Hello world"));
            });
            page.ViewContext.Writer = writer;

            // Act
            await page.ExecuteAsync();

            // Assert
            var buffer = writer.BufferedWriter.Entries;
            Assert.Equal(1, buffer.Count);
            Assert.Equal("Hello world", HtmlContentUtilities.HtmlContentToString(((IHtmlContent)buffer[0])));
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
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<div>Hello World!</div>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
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
                            tagMode: TagMode.StartTagAndEndTag,
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
                            tagMode: TagMode.StartTagAndEndTag,
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
                            tagMode: TagMode.StartTagAndEndTag,
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
                            attributes:  new TagHelperAttributeList()
                            {
                                ["test"] = new TagHelperAttribute
                                {
                                    Name = "test",
                                    Minimized = true
                                },
                            },
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p test>Hello World!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList()
                            {
                                ["test"] = new TagHelperAttribute
                                {
                                    Name = "test",
                                    Minimized = true
                                },
                                ["test2"] = new TagHelperAttribute
                                {
                                    Name = "test2",
                                    Minimized = true
                                },
                            },
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p test test2>Hello World!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList()
                            {
                                ["first"] = "unminimized",
                                ["test"] = new TagHelperAttribute
                                {
                                    Name = "test",
                                    Minimized = true
                                },
                            },
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p first=\"HtmlEncode[[unminimized]]\" test>Hello World!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList()
                            {
                                ["test"] = new TagHelperAttribute
                                {
                                    Name = "test",
                                    Minimized = true
                                },
                                ["last"] = "unminimized",
                            },
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p test last=\"HtmlEncode[[unminimized]]\">Hello World!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList() { { "test", "testVal" } },
                            tagMode: TagMode.SelfClosing,
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
                            tagMode: TagMode.SelfClosing,
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
                            attributes:  new TagHelperAttributeList() { { "test", "testVal" } },
                            tagMode: TagMode.StartTagOnly,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p test=\"HtmlEncode[[testVal]]\">"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList() { { "test", "testVal" }, { "something", "  spaced  " } },
                            tagMode: TagMode.StartTagOnly,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p test=\"HtmlEncode[[testVal]]\" something=\"HtmlEncode[[  spaced  ]]\">"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
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
                            tagMode: TagMode.StartTagAndEndTag,
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
                            tagMode: TagMode.StartTagAndEndTag,
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
                            tagMode: TagMode.StartTagAndEndTag,
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
                            tagMode: TagMode.SelfClosing,
                            preElement:  null,
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: null),
                        "<p />"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagOnly,
                            preElement:  null,
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: null),
                        "<p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
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
                            tagMode: TagMode.SelfClosing,
                            preElement:  null,
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: null),
                        "<random />"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "random",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagOnly,
                            preElement:  null,
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: null),
                        "<random>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  "Before",
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "Before<custom></custom>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  "Before",
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "Before"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            tagMode: TagMode.SelfClosing,
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
                            tagMode: TagMode.SelfClosing,
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
                            tagMode: TagMode.SelfClosing,
                            preElement:  "Before",
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "Before<custom />"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            tagMode: TagMode.StartTagOnly,
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
                            tagMode: TagMode.StartTagOnly,
                            preElement:  "Before",
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "Before<custom test=\"HtmlEncode[[testVal]]\">"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagOnly,
                            preElement:  "Before",
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "Before<custom>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: "After"),
                        "<custom></custom>After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: "After"),
                        "After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            tagMode: TagMode.SelfClosing,
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
                            tagMode: TagMode.SelfClosing,
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
                            tagMode: TagMode.SelfClosing,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: "After"),
                        "<custom />After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            tagMode: TagMode.StartTagOnly,
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
                            tagMode: TagMode.StartTagOnly,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: "After"),
                        "<custom test=\"HtmlEncode[[testVal]]\">After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagOnly,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: "After"),
                        "<custom>After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
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
                            tagMode: TagMode.StartTagAndEndTag,
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
                            tagMode: TagMode.SelfClosing,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "Before<custom />After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.SelfClosing,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "BeforeHelloTestWorld!After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagOnly,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "Before<custom>After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagOnly,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "BeforeHelloTestWorld!After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "BeforeHelloTestWorld!After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            tagMode: TagMode.StartTagAndEndTag,
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
                tagMode: output.TagMode,
                items: new Dictionary<object, object>(),
                uniqueId: string.Empty,
                executeChildContentAsync: () => Task.FromResult(result: true),
                startTagHelperWritingScope: () => { },
                endTagHelperWritingScope: () => new DefaultTagHelperContent());
            tagHelperExecutionContext.Output = output;

            // Act
            var page = CreatePage(p =>
            {
                p.HtmlEncoder = new HtmlTestEncoder();
                p.WriteTagHelperAsync(tagHelperExecutionContext).Wait();
            }, context);
            await page.ExecuteAsync();

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(writer.Content));
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
                tagMode: TagMode.StartTagAndEndTag,
                items: new Dictionary<object, object>(),
                uniqueId: string.Empty,
                executeChildContentAsync: () =>
                {
                    defaultTagHelperContent.AppendHtml(input);
                    return Task.FromResult(result: true);
                },
                startTagHelperWritingScope: () => { },
                endTagHelperWritingScope: () => defaultTagHelperContent);
            tagHelperExecutionContext.Output = new TagHelperOutput(
                tagName: "p",
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (_) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
            if (childContentRetrieved)
            {
                await tagHelperExecutionContext.GetChildContentAsync(useCachedResult: true);
            }

            // Act
            var page = CreatePage(p =>
            {
                p.HtmlEncoder = new HtmlTestEncoder();
                p.WriteTagHelperAsync(tagHelperExecutionContext).Wait();
            }, context);
            await page.ExecuteAsync();

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(writer.Content));
        }

        [Fact]
        public async Task WriteTagHelperToAsync_WritesToSpecifiedWriter()
        {
            // Arrange
            var writer = new StringCollectionTextWriter(Encoding.UTF8);
            var context = CreateViewContext(new StringWriter());
            var tagHelperExecutionContext = new TagHelperExecutionContext(
                tagName: "p",
                tagMode: TagMode.StartTagAndEndTag,
                items: new Dictionary<object, object>(),
                uniqueId: string.Empty,
                executeChildContentAsync: () => { return Task.FromResult(result: true); },
                startTagHelperWritingScope: () => { },
                endTagHelperWritingScope: () => new DefaultTagHelperContent());
            tagHelperExecutionContext.Output = new TagHelperOutput(
                tagName: "p",
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (_) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
            tagHelperExecutionContext.Output.Content.AppendHtml("Hello World!");

            // Act
            var page = CreatePage(p =>
            {
                p.HtmlEncoder = new HtmlTestEncoder();
                p.WriteTagHelperToAsync(writer, tagHelperExecutionContext).Wait();
            }, context);
            await page.ExecuteAsync();

            // Assert
            Assert.Equal("<p>Hello World!</p>", HtmlContentUtilities.HtmlContentToString(writer.Content));
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
                tagMode: output.TagMode,
                items: new Dictionary<object, object>(),
                uniqueId: string.Empty,
                executeChildContentAsync: () => Task.FromResult(result: true),
                startTagHelperWritingScope: () => { },
                endTagHelperWritingScope: () => new DefaultTagHelperContent());
            tagHelperExecutionContext.Output = output;

            // Act
            var page = CreatePage(p =>
            {
                p.HtmlEncoder = new HtmlTestEncoder();
                p.WriteTagHelperToAsync(writer, tagHelperExecutionContext).Wait();
            }, context);
            await page.ExecuteAsync();

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(writer.Content));
        }

        private static TagHelperOutput GetTagHelperOutput(
            string tagName,
            TagHelperAttributeList attributes,
            TagMode tagMode,
            string preElement,
            string preContent,
            string content,
            string postContent,
            string postElement)
        {
            var output = new TagHelperOutput(
                tagName,
                attributes,
                getChildContentAsync: (_) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()))
            {
                TagMode = tagMode
            };

            output.PreElement.AppendHtml(preElement);
            output.PreContent.AppendHtml(preContent);
            output.Content.AppendHtml(content);
            output.PostContent.AppendHtml(postContent);
            output.PostElement.AppendHtml(postElement);

            return output;
        }

        private static TestableRazorPage CreatePage(
            Action<TestableRazorPage> executeAction,
            ViewContext context = null)
        {
            return CreatePage(page =>
            {
                executeAction(page);
                return Task.FromResult(0);
            }, context);
        }


        private static TestableRazorPage CreatePage(
            Func<TestableRazorPage, Task> executeAction,
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
            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ActionDescriptor());
            return new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                new ViewDataDictionary(new EmptyModelMetadataProvider()),
                Mock.Of<ITempDataDictionary>(),
                writer,
                new HtmlHelperOptions());
        }

        private static Func<TextWriter, Task> CreateAsyncWriteDelegate(string value)
        {
            return async (writer) => await writer.WriteAsync(value);
        }

        public abstract class TestableRazorPage : RazorPage
        {
            public TestableRazorPage()
            {
                HtmlEncoder = new HtmlTestEncoder();
            }

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