// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Razor;

public class RazorPageTest
{
    private readonly RenderAsyncDelegate _nullRenderAsyncDelegate = () => Task.FromResult(0);
    private readonly Func<TextWriter, Task> NullAsyncWrite = writer => writer.WriteAsync(string.Empty);

    [Fact]
    public async Task WritingScopesRedirectContentWrittenToViewContextWriter()
    {
        // Arrange
        var viewContext = CreateViewContext();
        var page = CreatePage(v =>
        {
            v.HtmlEncoder = new HtmlTestEncoder();
            v.Write("Hello Prefix");
            v.StartTagHelperWritingScope(encoder: null);
            v.Write("Hello from Output");
            v.ViewContext.Writer.Write("Hello from view context writer");
            var scopeValue = v.EndTagHelperWritingScope();
            v.Write("From Scope: ");
            v.Write(scopeValue);
        });

        // Act
        await page.ExecuteAsync();
        var pageOutput = page.RenderedContent;

        // Assert
        Assert.Equal(
            "HtmlEncode[[Hello Prefix]]HtmlEncode[[From Scope: ]]HtmlEncode[[Hello from Output]]" +
            "Hello from view context writer",
            pageOutput);
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
            v.StartTagHelperWritingScope(encoder: null);
            v.Write("Hello In Scope");
            var scopeValue = v.EndTagHelperWritingScope();
            v.Write("From Scope: ");
            v.Write(scopeValue);
        });

        // Act
        await page.ExecuteAsync();
        var pageOutput = page.RenderedContent;

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
            v.StartTagHelperWritingScope(encoder: null);
            v.Write("Hello In Scope Pre Nest");

            v.StartTagHelperWritingScope(encoder: null);
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

        // Assert
        var pageOutput = page.RenderedContent;
        Assert.Equal(
            "HtmlEncode[[Hello Prefix]]HtmlEncode[[From Scopes: ]]HtmlEncode[[Hello In Scope Pre Nest]]" +
            "HtmlEncode[[Hello In Scope Post Nest]]HtmlEncode[[Hello In Nested Scope]]",
            pageOutput);
    }

    [Fact]
    public async Task StartTagHelperWritingScope_CannotFlushInWritingScope()
    {
        // Arrange
        var viewContext = CreateViewContext();
        var page = CreatePage(async v =>
        {
            v.Path = "/Views/TestPath/Test.cshtml";
            v.StartTagHelperWritingScope(encoder: null);
            await v.FlushAsync();
        });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => page.ExecuteAsync());
        Assert.Equal(
            "The FlushAsync operation cannot be performed while " +
            "inside a writing scope in '/Views/TestPath/Test.cshtml'.",
            ex.Message);
    }

    [Fact]
    public async Task StartTagHelperWritingScope_SetsHtmlEncoder()
    {
        // Arrange
        var encoder = Mock.Of<HtmlEncoder>();
        var page = CreatePage(v =>
        {
            v.StartTagHelperWritingScope(encoder);
        });

        // Act
        await page.ExecuteAsync();

        // Assert
        Assert.Same(encoder, page.HtmlEncoder);
    }

    [Fact]
    public async Task StartTagHelperWritingScope_DoesNotSetHtmlEncoderToNull()
    {
        // Arrange
        var page = CreatePage(v =>
        {
            v.StartTagHelperWritingScope(encoder: null);
        });
        var originalEncoder = page.HtmlEncoder;

        // Act
        await page.ExecuteAsync();

        // Assert
        Assert.NotNull(originalEncoder);
        Assert.Same(originalEncoder, page.HtmlEncoder);
    }

    [Fact]
    public async Task EndTagHelperWritingScope_CannotEndWritingScopeWhenNoWritingScope()
    {
        // Arrange
        var viewContext = CreateViewContext();
        var page = CreatePage(v =>
        {
            v.EndTagHelperWritingScope();
        });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => page.ExecuteAsync());
        Assert.Equal("There is no active writing scope to end.", ex.Message);
    }

    [Fact]
    public async Task EndTagHelperWritingScope_ReturnsAppropriateContent()
    {
        // Arrange
        var viewContext = CreateViewContext();
        var page = CreatePage(v =>
        {
            v.HtmlEncoder = new HtmlTestEncoder();
            v.StartTagHelperWritingScope(encoder: null);
            v.Write("Hello World!");
            var returnValue = v.EndTagHelperWritingScope();

            // Assert
            var content = Assert.IsType<DefaultTagHelperContent>(returnValue);
            Assert.Equal("HtmlEncode[[Hello World!]]", content.GetContent());
        });

        // Act & Assert
        await page.ExecuteAsync();
    }

    [Fact]
    public async Task EndWriteTagHelperAttribute_RestoresPageWriter()
    {
        // Arrange
        var page = CreatePage(v =>
        {
            v.BeginWriteTagHelperAttribute();
            v.Write("Hello World!");
            v.EndWriteTagHelperAttribute();
        });
        var originalWriter = page.Output;

        // Act
        await page.ExecuteAsync();

        // Assert
        Assert.NotNull(originalWriter);
        Assert.Same(originalWriter, page.Output);
    }

    [Fact]
    public async Task EndWriteTagHelperAttribute_ReturnsAppropriateContent()
    {
        // Arrange
        var viewContext = CreateViewContext();
        var page = CreatePage(v =>
        {
            v.HtmlEncoder = new HtmlTestEncoder();
            v.BeginWriteTagHelperAttribute();
            v.Write("Hello World!");
            var returnValue = v.EndWriteTagHelperAttribute();

            // Assert
            var content = Assert.IsType<string>(returnValue);
            Assert.Equal("HtmlEncode[[Hello World!]]", content);
        });

        // Act & Assert
        await page.ExecuteAsync();
    }

    [Fact]
    public async Task BeginWriteTagHelperAttribute_NestingWritingScopesThrows()
    {
        // Arrange
        var viewContext = CreateViewContext();
        var page = CreatePage(v =>
        {
            v.BeginWriteTagHelperAttribute();
            v.BeginWriteTagHelperAttribute();
            v.Write("Hello World!");
        });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => page.ExecuteAsync());
        Assert.Equal("Nesting of TagHelper attribute writing scopes is not supported.", ex.Message);
    }

    // This is an integration test for ensuring that ViewBuffer segments used by
    // TagHelpers can be merged back into the 'main' segment where possible.
    [Fact]
    public async Task TagHelperScopes_ViewBuffersCanCombine()
    {
        // Arrange
        var bufferScope = new TestViewBufferScope();
        var viewContext = CreateViewContext(bufferScope: bufferScope);

        var page = CreatePage(async v =>
        {
            Assert.Empty(bufferScope.CreatedBuffers);
            v.Write("Level:0"); // Creates a 'top-level' buffer.
            Assert.Single(bufferScope.CreatedBuffers);

            // Run a TagHelper
            {
                v.StartTagHelperWritingScope(encoder: null);

                Assert.Single(bufferScope.CreatedBuffers);
                Assert.Empty(bufferScope.ReturnedBuffers);
                v.Write("Level:1-A"); // Creates a new buffer for the TagHelper.
                Assert.Equal(2, bufferScope.CreatedBuffers.Count);
                Assert.Empty(bufferScope.ReturnedBuffers);

                TagHelperContent innerContentLevel1 = null;
                var outputLevel1 = new TagHelperOutput("t1", new TagHelperAttributeList(), (_, encoder) =>
                {
                    return Task.FromResult(innerContentLevel1);
                });

                innerContentLevel1 = v.EndTagHelperWritingScope();
                outputLevel1.Content = await outputLevel1.GetChildContentAsync();

                Assert.Equal(2, bufferScope.CreatedBuffers.Count);
                Assert.Empty(bufferScope.ReturnedBuffers);
                v.Write(outputLevel1); // Writing the TagHelper to output returns a buffer.
                Assert.Equal(2, bufferScope.CreatedBuffers.Count);
                Assert.Single(bufferScope.ReturnedBuffers);
            }

            Assert.Equal(2, bufferScope.CreatedBuffers.Count);
            Assert.Single(bufferScope.ReturnedBuffers);
            v.Write("Level:0"); // Already have a buffer for this scope.
            Assert.Equal(2, bufferScope.CreatedBuffers.Count);
            Assert.Single(bufferScope.ReturnedBuffers);

            // Run another TagHelper
            {
                v.StartTagHelperWritingScope(encoder: null);

                Assert.Equal(2, bufferScope.CreatedBuffers.Count);
                Assert.Single(bufferScope.ReturnedBuffers);
                v.Write("Level:1-B"); // Creates a new buffer for the TagHelper.
                Assert.Equal(3, bufferScope.CreatedBuffers.Count);
                Assert.Single(bufferScope.ReturnedBuffers);

                TagHelperContent innerContentLevel1 = null;
                var outputLevel1 = new TagHelperOutput("t2", new TagHelperAttributeList(), (_, encoder) =>
                {
                    return Task.FromResult(innerContentLevel1);
                });

                // Run a nested TagHelper
                {
                    v.StartTagHelperWritingScope(encoder: null);

                    Assert.Equal(3, bufferScope.CreatedBuffers.Count);
                    Assert.Single(bufferScope.ReturnedBuffers);
                    v.Write("Level:2"); // Creates a new buffer for the TagHelper.
                    Assert.Equal(4, bufferScope.CreatedBuffers.Count);
                    Assert.Single(bufferScope.ReturnedBuffers);

                    TagHelperContent innerContentLevel2 = null;
                    var outputLevel2 = new TagHelperOutput("t3", new TagHelperAttributeList(), (_, encoder) =>
                    {
                        return Task.FromResult(innerContentLevel2);
                    });

                    innerContentLevel2 = v.EndTagHelperWritingScope();
                    outputLevel2.Content = await outputLevel2.GetChildContentAsync();

                    Assert.Equal(4, bufferScope.CreatedBuffers.Count);
                    Assert.Single(bufferScope.ReturnedBuffers);
                    v.Write(outputLevel2); // Writing the TagHelper to output returns a buffer.
                    Assert.Equal(4, bufferScope.CreatedBuffers.Count);
                    Assert.Equal(2, bufferScope.ReturnedBuffers.Count);
                }

                Assert.Equal(4, bufferScope.CreatedBuffers.Count);
                Assert.Equal(2, bufferScope.ReturnedBuffers.Count);
                v.Write("Level:1-B"); // Already have a buffer for this scope.
                Assert.Equal(4, bufferScope.CreatedBuffers.Count);
                Assert.Equal(2, bufferScope.ReturnedBuffers.Count);

                innerContentLevel1 = v.EndTagHelperWritingScope();
                outputLevel1.Content = await outputLevel1.GetChildContentAsync();

                Assert.Equal(4, bufferScope.CreatedBuffers.Count);
                Assert.Equal(2, bufferScope.ReturnedBuffers.Count);
                v.Write(outputLevel1); // Writing the TagHelper to output returns a buffer.
                Assert.Equal(4, bufferScope.CreatedBuffers.Count);
                Assert.Equal(3, bufferScope.ReturnedBuffers.Count);
            }

            Assert.Equal(4, bufferScope.CreatedBuffers.Count);
            Assert.Equal(3, bufferScope.ReturnedBuffers.Count);
            v.Write("Level:0"); // Already have a buffer for this scope.
            Assert.Equal(4, bufferScope.CreatedBuffers.Count);
            Assert.Equal(3, bufferScope.ReturnedBuffers.Count);

        }, viewContext);

        // Act & Assert
        await page.ExecuteAsync();
        Assert.Equal(
            "HtmlEncode[[Level:0]]" +
            "<t1>" +
                "HtmlEncode[[Level:1-A]]" +
            "</t1>" +
            "HtmlEncode[[Level:0]]" +
            "<t2>" +
                "HtmlEncode[[Level:1-B]]" +
                "<t3>" +
                    "HtmlEncode[[Level:2]]" +
                "</t3>" +
                "HtmlEncode[[Level:1-B]]" +
            "</t2>" +
            "HtmlEncode[[Level:0]]",
            page.RenderedContent);
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

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => page.ExecuteAsync());
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
                { "bar", () => page.Output.WriteAsync(expected) }
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

        // Act & Assert
        await page.ExecuteAsync();
        Assert.Equal("RenderSection invocation in '/Views/TestPath/Test.cshtml' is invalid. " +
            "RenderSection can only be called from a layout page.",
            ex.Message);
    }

    [Fact]
    public async Task RenderSection_ThrowsIfRequiredSectionIsNotFound()
    {
        // Arrange
        var context = CreateViewContext(viewPath: "/Views/TestPath/Test.cshtml");
        context.ExecutingFilePath = "/Views/Shared/_Layout.cshtml";
        var page = CreatePage(v =>
        {
            v.RenderSection("bar");
        }, context: context);
        page.PreviousSectionWriters = new Dictionary<string, RenderAsyncDelegate>
            {
                { "baz", _nullRenderAsyncDelegate }
            };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => page.ExecuteAsync());
        var message = $"The layout page '/Views/Shared/_Layout.cshtml' cannot find the section 'bar'" +
            " in the content page '/Views/TestPath/Test.cshtml'.";
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public async Task IgnoreSection_DoesNotThrowIfSectionIsNotFound()
    {
        // Arrange
        var context = CreateViewContext(viewPath: "/Views/TestPath/Test.cshtml");
        context.ExecutingFilePath = "/Views/Shared/_Layout.cshtml";
        var page = CreatePage(v =>
        {
            v.Path = "/Views/TestPath/Test.cshtml";
            v.IgnoreSection("bar");
        }, context);
        page.PreviousSectionWriters = new Dictionary<string, RenderAsyncDelegate>
            {
                { "baz", _nullRenderAsyncDelegate }
            };

        // Act & Assert
        await page.ExecuteAsync();
        // Nothing to assert, just getting here is validation enough.
    }

    [Fact]
    public void IsSectionDefined_ThrowsIfPreviousSectionWritersIsNotRegistered()
    {
        // Arrange
        var page = CreatePage(v =>
        {
            v.Path = "/Views/TestPath/Test.cshtml";
        });
        page.ExecuteAsync();

        // Act & Assert
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
        page.BodyContent = new HtmlString("body-content");

        // Act
        await page.ExecuteAsync();

        // Assert
        Assert.False(actual);
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
        page.BodyContent = new HtmlString("body-content");

        // Act
        await page.ExecuteAsync();

        // Assert
        Assert.True(actual);
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

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(page.ExecuteAsync);
        Assert.Equal(
            "RenderSectionAsync invocation in '/Views/TestPath/Test.cshtml' is invalid." +
            " The section 'header' has already been rendered.",
            ex.Message);
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

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(page.ExecuteAsync);
        Assert.Equal(
            "RenderSectionAsync invocation in '/Views/TestPath/Test.cshtml' is invalid." +
            " The section 'header' has already been rendered.",
            ex.Message);
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

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(page.ExecuteAsync);
        Assert.Equal(
            "RenderSectionAsync invocation in '/Views/TestPath/Test.cshtml' is invalid." +
            " The section 'header' has already been rendered.",
            ex.Message);
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

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(page.ExecuteAsync);
        Assert.Equal(
            "RenderSectionAsync invocation in '/Views/TestPath/Test.cshtml' is invalid. " +
            "RenderSectionAsync can only be called from a layout page.",
            ex.Message);
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
        page.BodyContent = new HtmlString("some content");
        await page.ExecuteAsync();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => page.EnsureRenderedBodyOrSections());
        Assert.Equal($"RenderBody has not been called for the page at '{path}'. To ignore call IgnoreBody().", ex.Message);
    }

    [Fact]
    public async Task EnsureRenderedBodyOrSections_SucceedsIfRenderBodyIsNotCalledFromPage_AndNoSectionsAreDefined_AndBodyIgnored()
    {
        // Arrange
        var path = "page-path";
        var page = CreatePage(v =>
        {
        });
        page.Path = path;
        page.BodyContent = new HtmlString("some content");
        page.IgnoreBody();

        // Act & Assert (does not throw)
        await page.ExecuteAsync();
        page.EnsureRenderedBodyOrSections();
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
        page.BodyContent = new HtmlString("some content");
        page.PreviousSectionWriters = new Dictionary<string, RenderAsyncDelegate>
            {
                { sectionName, _nullRenderAsyncDelegate }
            };
        await page.ExecuteAsync();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => page.EnsureRenderedBodyOrSections());
        Assert.Equal(
            "The following sections have been defined but have not been rendered by the page at " +
            $"'{path}': '{sectionName}'. To ignore an unrendered section call IgnoreSection(\"sectionName\").",
            ex.Message);
    }

    [Fact]
    public async Task EnsureRenderedBodyOrSections_SucceedsIfDefinedSectionsAreNotRendered_AndIgnored()
    {
        // Arrange
        var path = "page-path";
        var sectionName = "sectionA";
        var page = CreatePage(v =>
        {
        });
        page.Path = path;
        page.BodyContent = new HtmlString("some content");
        page.PreviousSectionWriters = new Dictionary<string, RenderAsyncDelegate>
            {
                { sectionName, _nullRenderAsyncDelegate }
            };
        page.IgnoreSection(sectionName);

        // Act & Assert (does not throw)
        await page.ExecuteAsync();
        page.EnsureRenderedBodyOrSections();
    }

    [Fact]
    public async Task ExecuteAsync_RendersSectionsThatAreNotIgnored()
    {
        // Arrange
        var path = "page-path";
        var page = CreatePage(async p =>
        {
            p.IgnoreSection("ignored");
            p.Write(await p.RenderSectionAsync("not-ignored-section"));
        });
        page.Path = path;
        page.BodyContent = new HtmlString("some content");
        page.PreviousSectionWriters = new Dictionary<string, RenderAsyncDelegate>
            {
                { "ignored", _nullRenderAsyncDelegate },
                { "not-ignored-section", () => page.Output.WriteAsync("not-ignored-section-content") }
            };

        // Act
        await page.ExecuteAsync();

        // Assert
        Assert.Equal("not-ignored-section-content", page.RenderedContent);
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
        page.BodyContent = new HtmlString("some content");
        page.PreviousSectionWriters = new Dictionary<string, RenderAsyncDelegate>
            {
                { sectionA, _nullRenderAsyncDelegate },
                { sectionB, _nullRenderAsyncDelegate },
            };

        // Act & Assert (does not throw)
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
        page.BodyContent = new HtmlString("body content" + Environment.NewLine);
        page.PreviousSectionWriters = new Dictionary<string, RenderAsyncDelegate>
            {
                {
                    "footer", () => page.Output.WriteLineAsync("Footer section")
                },
                {
                    "header", () => page.Output.WriteLineAsync("Header section")
                },
                {
                    "async-header", () => page.Output.WriteLineAsync("Async Header section")
                },
                {
                    "async-footer", () => page.Output.WriteLineAsync("Async Footer section")
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
        helper
            .Setup(h => h.Content("url"))
            .Returns(expected)
            .Verifiable();
        var factory = new Mock<IUrlHelperFactory>();
        factory
            .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(helper.Object);

        var page = CreatePage(v =>
        {
            v.HtmlEncoder = new HtmlTestEncoder();
            v.Write(v.Href("url"));
        });
        var services = new Mock<IServiceProvider>();
        services.Setup(s => s.GetService(typeof(IUrlHelperFactory)))
                 .Returns(factory.Object);
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

        // Act & Assert
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
            p.DefineSection("test-section", async () =>
            {
                await p.FlushAsync();
            });
        }, context);

        // Act
        await page.ExecuteAsync();
        page.IsLayoutBeingRendered = true;

        // Assert (does not throw)
        var renderAsyncDelegate = page.SectionWriters["test-section"];
        await renderAsyncDelegate();
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

    public static TheoryData<string, int, object, int, bool, string> AddHtmlAttributeValues_ValueData
    {
        get
        {
            // attributeValues, expectedValue
            return new TheoryData<string, int, object, int, bool, string>
                {
                    {
                        string.Empty, 9, (object)"Hello", 9, true, "Hello"
                    },
                    {
                        " ", 9, (object)"Hello", 10, true, " Hello"
                    },
                    {

                        " ", 9, (object)null, 10, false, string.Empty
                    },
                    {
                        " ", 9, (object)false, 10, false, " HtmlEncode[[False]]"
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(AddHtmlAttributeValues_ValueData))]
    public void AddHtmlAttributeValues_AddsToHtmlAttributesAsExpected(
        string prefix,
        int prefixOffset,
        object value,
        int valueOffset,
        bool isLiteral,
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
            startTagHelperWritingScope: _ => { },
            endTagHelperWritingScope: () => new DefaultTagHelperContent());

        // Act
        page.BeginAddHtmlAttributeValues(executionContext, "someattr", 1, HtmlAttributeValueStyle.SingleQuotes);
        page.AddHtmlAttributeValue(prefix, prefixOffset, value, valueOffset, 0, isLiteral);
        page.EndAddHtmlAttributeValues(executionContext);

        // Assert
        var output = executionContext.Output;
        var htmlAttribute = Assert.Single(output.Attributes);
        Assert.Equal("someattr", htmlAttribute.Name, StringComparer.Ordinal);
        var htmlContent = Assert.IsAssignableFrom<IHtmlContent>(htmlAttribute.Value);
        Assert.Equal(expectedValue, HtmlContentUtilities.HtmlContentToString(htmlContent), StringComparer.Ordinal);
        Assert.Equal(HtmlAttributeValueStyle.SingleQuotes, htmlAttribute.ValueStyle);

        var context = executionContext.Context;
        var allAttribute = Assert.Single(context.AllAttributes);
        Assert.Equal("someattr", allAttribute.Name, StringComparer.Ordinal);
        htmlContent = Assert.IsAssignableFrom<IHtmlContent>(allAttribute.Value);
        Assert.Equal(expectedValue, HtmlContentUtilities.HtmlContentToString(htmlContent), StringComparer.Ordinal);
        Assert.Equal(HtmlAttributeValueStyle.SingleQuotes, allAttribute.ValueStyle);
    }

    [Fact]
    public void AddHtmlAttributeValues_TwoAttributeValues_AddsToHtmlAttributesAsExpected()
    {
        // Arrange
        var expectedValue = "  HtmlEncode[[True]]  abcd";
        var attributeValues = new[]
        {
                Tuple.Create("  ", 9, (object)true, 11, false),
                Tuple.Create("  ", 9, (object)"abcd", 17, true)
            };
        var page = CreatePage(p => { });
        page.HtmlEncoder = new HtmlTestEncoder();
        var executionContext = new TagHelperExecutionContext(
            "p",
            tagMode: TagMode.StartTagAndEndTag,
            items: new Dictionary<object, object>(),
            uniqueId: string.Empty,
            executeChildContentAsync: () => Task.FromResult(result: true),
            startTagHelperWritingScope: _ => { },
            endTagHelperWritingScope: () => new DefaultTagHelperContent());

        // Act
        page.BeginAddHtmlAttributeValues(executionContext, "someattr", attributeValues.Length, HtmlAttributeValueStyle.SingleQuotes);
        foreach (var value in attributeValues)
        {
            page.AddHtmlAttributeValue(value.Item1, value.Item2, value.Item3, value.Item4, 0, value.Item5);
        }
        page.EndAddHtmlAttributeValues(executionContext);

        // Assert
        var output = executionContext.Output;
        var htmlAttribute = Assert.Single(output.Attributes);
        Assert.Equal("someattr", htmlAttribute.Name, StringComparer.Ordinal);
        var htmlContent = Assert.IsAssignableFrom<IHtmlContent>(htmlAttribute.Value);
        Assert.Equal(expectedValue, HtmlContentUtilities.HtmlContentToString(htmlContent), StringComparer.Ordinal);
        Assert.Equal(HtmlAttributeValueStyle.SingleQuotes, htmlAttribute.ValueStyle);

        var context = executionContext.Context;
        var allAttribute = Assert.Single(context.AllAttributes);
        Assert.Equal("someattr", allAttribute.Name, StringComparer.Ordinal);
        htmlContent = Assert.IsAssignableFrom<IHtmlContent>(allAttribute.Value);
        Assert.Equal(expectedValue, HtmlContentUtilities.HtmlContentToString(htmlContent), StringComparer.Ordinal);
        Assert.Equal(HtmlAttributeValueStyle.SingleQuotes, allAttribute.ValueStyle);
    }

    [Fact]
    public void AddHtmlAttributeValues_ThreeAttributeValues_AddsToHtmlAttributesAsExpected()
    {
        // Arrange
        var expectedValue = "prefix HtmlEncode[[suffix]]";
        var attributeValues = new[]
        {
                Tuple.Create(string.Empty, 9, (object)"prefix", 9, true),
                Tuple.Create("  ", 15, (object)null, 17, false),
                Tuple.Create(" ", 21, (object)"suffix", 22, false),
            };
        var page = CreatePage(p => { });
        page.HtmlEncoder = new HtmlTestEncoder();
        var executionContext = new TagHelperExecutionContext(
            "p",
            tagMode: TagMode.StartTagAndEndTag,
            items: new Dictionary<object, object>(),
            uniqueId: string.Empty,
            executeChildContentAsync: () => Task.FromResult(result: true),
            startTagHelperWritingScope: _ => { },
            endTagHelperWritingScope: () => new DefaultTagHelperContent());

        // Act
        page.BeginAddHtmlAttributeValues(executionContext, "someattr", attributeValues.Length, HtmlAttributeValueStyle.SingleQuotes);
        foreach (var value in attributeValues)
        {
            page.AddHtmlAttributeValue(value.Item1, value.Item2, value.Item3, value.Item4, 0, value.Item5);
        }
        page.EndAddHtmlAttributeValues(executionContext);

        // Assert
        var output = executionContext.Output;
        var htmlAttribute = Assert.Single(output.Attributes);
        Assert.Equal("someattr", htmlAttribute.Name, StringComparer.Ordinal);
        var htmlContent = Assert.IsAssignableFrom<IHtmlContent>(htmlAttribute.Value);
        Assert.Equal(expectedValue, HtmlContentUtilities.HtmlContentToString(htmlContent), StringComparer.Ordinal);
        Assert.Equal(HtmlAttributeValueStyle.SingleQuotes, htmlAttribute.ValueStyle);

        var context = executionContext.Context;
        var allAttribute = Assert.Single(context.AllAttributes);
        Assert.Equal("someattr", allAttribute.Name, StringComparer.Ordinal);
        htmlContent = Assert.IsAssignableFrom<IHtmlContent>(allAttribute.Value);
        Assert.Equal(expectedValue, HtmlContentUtilities.HtmlContentToString(htmlContent), StringComparer.Ordinal);
        Assert.Equal(HtmlAttributeValueStyle.SingleQuotes, allAttribute.ValueStyle);
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
            startTagHelperWritingScope: _ => { },
            endTagHelperWritingScope: () => new DefaultTagHelperContent());

        // Act
        page.BeginAddHtmlAttributeValues(executionContext, "someattr", 1, HtmlAttributeValueStyle.DoubleQuotes);
        page.AddHtmlAttributeValue(string.Empty, 9, attributeValue, 9, valueLength: 0, isLiteral: false);
        page.EndAddHtmlAttributeValues(executionContext);

        // Assert
        var output = executionContext.Output;
        Assert.Empty(output.Attributes);
        var context = executionContext.Context;
        var attribute = Assert.Single(context.AllAttributes);
        Assert.Equal("someattr", attribute.Name, StringComparer.Ordinal);
        Assert.Equal(expectedValue, (string)attribute.Value, StringComparer.Ordinal);
        Assert.Equal(HtmlAttributeValueStyle.DoubleQuotes, attribute.ValueStyle);
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
            startTagHelperWritingScope: _ => { },
            endTagHelperWritingScope: () => new DefaultTagHelperContent());

        // Act
        page.BeginAddHtmlAttributeValues(executionContext, "someattr", 1, HtmlAttributeValueStyle.NoQuotes);
        page.AddHtmlAttributeValue(string.Empty, 9, true, 9, valueLength: 0, isLiteral: false);
        page.EndAddHtmlAttributeValues(executionContext);

        // Assert
        var output = executionContext.Output;
        var htmlAttribute = Assert.Single(output.Attributes);
        Assert.Equal("someattr", htmlAttribute.Name, StringComparer.Ordinal);
        Assert.Equal("someattr", (string)htmlAttribute.Value, StringComparer.Ordinal);
        Assert.Equal(HtmlAttributeValueStyle.NoQuotes, htmlAttribute.ValueStyle);
        var context = executionContext.Context;
        var allAttribute = Assert.Single(context.AllAttributes);
        Assert.Equal("someattr", allAttribute.Name, StringComparer.Ordinal);
        Assert.Equal("someattr", (string)allAttribute.Value, StringComparer.Ordinal);
        Assert.Equal(HtmlAttributeValueStyle.NoQuotes, allAttribute.ValueStyle);
    }

    public static TheoryData<string, int, object, int, bool, string> WriteAttributeData
    {
        get
        {
            // AttributeValues, ExpectedOutput
            return new TheoryData<string, int, object, int, bool, string>
                {
                    {
                        string.Empty, 9, (object)true, 9, false, "someattr=HtmlEncode[[someattr]]"
                    },
                    {
                        string.Empty, 9, (object)false, 9, false, string.Empty
                    },
                    {
                        string.Empty, 9, (object)null, 9, false, string.Empty
                    },
                    {
                        "  ", 9, (object)false, 11, false, "someattr=  HtmlEncode[[False]]"
                    },
                    {
                        "  ", 9, (object)null, 11, false, "someattr="
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(WriteAttributeData))]
    public void WriteAttribute_UsesSpecifiedWriter_WritesAsExpected(
        string prefix,
        int prefixOffset,
        object value,
        int valueOffset,
        bool isLiteral,
        string expectedOutput)
    {
        // Arrange
        var page = CreatePage(p => { });
        page.HtmlEncoder = new HtmlTestEncoder();
        var writer = new StringWriter();
        var suffix = string.Empty;

        // Act
        page.PushWriter(writer);
        page.BeginWriteAttribute("someattr", "someattr=", 0, suffix, 0, 1);
        page.WriteAttributeValue(
            prefix,
            prefixOffset,
            value,
            valueOffset,
            value?.ToString().Length ?? 0,
            isLiteral);
        page.EndWriteAttribute();
        page.PopWriter();

        // Assert
        Assert.Equal(expectedOutput, writer.ToString());
    }

    [Fact]
    public void WriteAttribute_MultipleValues_UsesSpecifiedWriter_WritesAsExpected()
    {
        // Arrange
        var attributeValues = new[]
        {
                Tuple.Create("  ", 9, (object)true, 11, false),
                Tuple.Create("  ", 15, (object)"abcd", 17, true),
            };
        var expectedOutput = "someattr=  HtmlEncode[[True]]  abcd";

        var page = CreatePage(p => { });
        page.HtmlEncoder = new HtmlTestEncoder();
        var writer = new StringWriter();
        var prefix = "someattr=";
        var suffix = string.Empty;

        // Act
        page.PushWriter(writer);
        page.BeginWriteAttribute("someattr", prefix, 0, suffix, 0, attributeValues.Length);
        foreach (var value in attributeValues)
        {
            page.WriteAttributeValue(
                value.Item1,
                value.Item2,
                value.Item3,
                value.Item4,
                value.Item3?.ToString().Length ?? 0,
                value.Item5);
        }
        page.EndWriteAttribute();
        page.PopWriter();

        // Assert
        Assert.Equal(expectedOutput, writer.ToString());
    }

    [Fact]
    public void PushWriter_SetsUnderlyingWriter()
    {
        // Arrange
        var page = CreatePage(p => { });
        var writer = new StringWriter();

        // Act
        page.PushWriter(writer);

        // Assert
        Assert.Same(writer, page.ViewContext.Writer);
    }

    [Fact]
    public void PopWriter_ResetsUnderlyingWriter()
    {
        // Arrange
        var page = CreatePage(p => { });
        var defaultWriter = new StringWriter();
        page.ViewContext.Writer = defaultWriter;

        var writer = new StringWriter();

        // Act 1
        page.PushWriter(writer);

        // Assert 1
        Assert.Same(writer, page.ViewContext.Writer);

        // Act 2
        var poppedWriter = page.PopWriter();

        // Assert 2
        Assert.Same(defaultWriter, poppedWriter);
        Assert.Same(defaultWriter, page.ViewContext.Writer);
    }

    [Fact]
    public void WriteLiteral_NullValue_DoesNothing()
    {
        // Arrange
        var page = CreatePage(p => { });
        var defaultWriter = new StringWriter();
        page.ViewContext.Writer = defaultWriter;

        // Act
        page.WriteLiteral((object)null);

        // Assert - does not throw
        Assert.Empty(defaultWriter.ToString());
    }

    [Fact]
    public void WriteLiteral_BuffersResultToPushedWriter()
    {
        // Arrange
        var page = CreatePage(p => { });
        var defaultWriter = new StringWriter();
        page.ViewContext.Writer = defaultWriter;

        var bufferWriter = new StringWriter();

        // Act
        page.WriteLiteral("Not");
        page.PushWriter(bufferWriter);
        page.WriteLiteral("This should be buffered");
        page.PopWriter();
        page.WriteLiteral(" buffered");

        // Assert
        Assert.Equal("Not buffered", defaultWriter.ToString());
        Assert.Equal("This should be buffered", bufferWriter.ToString());
    }

    [Fact]
    public void Write_StringValue_UsesSpecifiedWriter_EncodesValue()
    {
        // Arrange
        var page = CreatePage(p => { });
        var bufferWriter = new StringWriter();

        // Act
        page.PushWriter(bufferWriter);
        page.Write("This should be encoded");
        page.PopWriter();

        // Assert
        Assert.Equal("HtmlEncode[[This should be encoded]]", bufferWriter.ToString());
    }

    [Fact]
    public async Task Write_WithHtmlString_WritesValueWithoutEncoding()
    {
        // Arrange
        var buffer = new ViewBuffer(new TestViewBufferScope(), string.Empty, pageSize: 32);
        var writer = new ViewBufferTextWriter(buffer, Encoding.UTF8);

        var page = CreatePage(p =>
        {
            p.Write(new HtmlString("Hello world"));
        });
        page.ViewContext.Writer = writer;

        // Act
        await page.ExecuteAsync();

        // Assert
        Assert.Equal("Hello world", HtmlContentUtilities.HtmlContentToString(buffer));
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

    private static ViewContext CreateViewContext(
        TextWriter writer = null,
        IViewBufferScope bufferScope = null,
        string viewPath = null)
    {
        bufferScope = bufferScope ?? new TestViewBufferScope();
        var buffer = new ViewBuffer(bufferScope, viewPath ?? "TEST", 32);
        writer = writer ?? new ViewBufferTextWriter(buffer, Encoding.UTF8);

        var httpContext = new DefaultHttpContext();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IViewBufferScope>(bufferScope)
            .BuildServiceProvider();
        httpContext.RequestServices = serviceProvider;
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());
        var viewMock = new Mock<IView>();
        if (!string.IsNullOrEmpty(viewPath))
        {
            viewMock.Setup(v => v.Path).Returns(viewPath);
        }
        return new ViewContext(
            actionContext,
            viewMock.Object,
            new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            Mock.Of<ITempDataDictionary>(),
            writer,
            new HtmlHelperOptions());
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
                var bufferedWriter = Assert.IsType<ViewBufferTextWriter>(Output);
                using (var stringWriter = new StringWriter())
                {
                    bufferedWriter.Buffer.WriteTo(stringWriter, HtmlEncoder);
                    return stringWriter.ToString();
                }
            }
        }

        public IHtmlContent RenderBodyPublic()
        {
            return base.RenderBody();
        }
    }
}
