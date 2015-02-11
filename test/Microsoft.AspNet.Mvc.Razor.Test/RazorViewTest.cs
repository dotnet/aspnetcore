// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.PageExecutionInstrumentation;
using Microsoft.Framework.WebEncoders;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorViewTest
    {
        private const string LayoutPath = "~/Shared/_Layout.cshtml";

#pragma warning disable 1998
        private readonly RenderAsyncDelegate _nullRenderAsyncDelegate = async writer => { };
#pragma warning restore 1998

        [Fact]
        public async Task RenderAsync_AsPartial_BuffersOutput()
        {
            // Arrange
            TextWriter actual = null;
            var page = new TestableRazorPage(v =>
            {
                actual = v.Output;
                v.HtmlEncoder = new HtmlEncoder();
                v.Write("Hello world");
            });
            var view = new RazorView(Mock.Of<IRazorViewEngine>(),
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider(),
                                     page,
                                     isPartial: true);
            var viewContext = CreateViewContext(view);
            var expected = viewContext.Writer;

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.NotSame(expected, actual);
            Assert.IsAssignableFrom<IBufferedTextWriter>(actual);
            Assert.Equal("Hello world", viewContext.Writer.ToString());
        }

        [Fact]
        public async Task RenderAsync_AsPartial_ActivatesViews_WithThePassedInViewContext()
        {
            // Arrange
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
            var page = new TestableRazorPage(v =>
            {
                // viewData is assigned to ViewContext by the activator
                Assert.Same(viewData, v.ViewContext.ViewData);
            });
            var activator = new Mock<IRazorPageActivator>();
            var view = new RazorView(Mock.Of<IRazorViewEngine>(),
                                     activator.Object,
                                     CreateViewStartProvider(),
                                     page,
                                     isPartial: true);

            var viewContext = CreateViewContext(view);
            var expectedWriter = viewContext.Writer;
            activator.Setup(a => a.Activate(page, It.IsAny<ViewContext>()))
                     .Callback((IRazorPage p, ViewContext c) =>
                     {
                         Assert.Same(c, viewContext);
                         c.ViewData = viewData;
                     })
                     .Verifiable();

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            activator.Verify();
            Assert.Same(expectedWriter, viewContext.Writer);
        }

        [Fact]
        public async Task ViewContext_ExecutingPagePath_ReturnsPathOfRazorPageBeingExecuted()
        {
            // Arrange
            var pagePath = "/my/view";
            var paths = new List<string>();
            var page = new TestableRazorPage(v =>
            {
                paths.Add(v.ViewContext.ExecutingFilePath);
                Assert.Equal(pagePath, v.ViewContext.View.Path);
            })
            {
                Path = pagePath
            };

            var viewStart = new TestableRazorPage(v =>
            {
                v.Layout = LayoutPath;
                paths.Add(v.ViewContext.ExecutingFilePath);
                Assert.Equal(pagePath, v.ViewContext.View.Path);
            })
            {
                Path = "_ViewStart"
            };

            var layout = new TestableRazorPage(v =>
            {
                v.RenderBodyPublic();
                paths.Add(v.ViewContext.ExecutingFilePath);
                Assert.Equal(pagePath, v.ViewContext.View.Path);
            })
            {
                Path = LayoutPath
            };

            var activator = Mock.Of<IRazorPageActivator>();
            var viewEngine = new Mock<IRazorViewEngine>();
            viewEngine.Setup(v => v.FindPage(It.IsAny<ActionContext>(), LayoutPath))
                      .Returns(new RazorPageResult(LayoutPath, layout));
            var view = new RazorView(viewEngine.Object,
                                     activator,
                                     CreateViewStartProvider(viewStart),
                                     page,
                                     isPartial: false);

            var viewContext = CreateViewContext(view);
            var expectedWriter = viewContext.Writer;

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.Equal(new[] { "_ViewStart", pagePath, LayoutPath }, paths);
        }

        [Fact]
        public async Task RenderAsync_AsPartial_ActivatesViews()
        {
            // Arrange
            var page = new TestableRazorPage(v => { });
            var activator = new Mock<IRazorPageActivator>();
            activator.Setup(a => a.Activate(page, It.IsAny<ViewContext>()))
                     .Verifiable();
            var view = new RazorView(Mock.Of<IRazorViewEngine>(),
                                     activator.Object,
                                     CreateViewStartProvider(),
                                     page,
                                     isPartial: true);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            activator.Verify();
        }

        [Fact]
        public async Task RenderAsync_AsPartial_ExecutesLayout_ButNotViewStartPages()
        {
            // Arrange
            var htmlEncoder = new HtmlEncoder();
            var expected = string.Join(htmlEncoder.HtmlEncode(Environment.NewLine),
                                       "layout-content",
                                       "page-content");
            var page = new TestableRazorPage(v =>
            {
                v.HtmlEncoder = htmlEncoder;
                v.Layout = LayoutPath;
                v.Write("page-content");
            });

            var layout = new TestableRazorPage(v =>
            {
                v.HtmlEncoder = htmlEncoder;
                v.Write("layout-content" + Environment.NewLine);
                v.RenderBodyPublic();
            });
            var pageFactory = new Mock<IRazorPageFactory>();
            pageFactory.Setup(p => p.CreateInstance(LayoutPath))
                       .Returns(layout);

            var viewEngine = new Mock<IRazorViewEngine>();
            viewEngine.Setup(v => v.FindPage(It.IsAny<ActionContext>(), LayoutPath))
                      .Returns(new RazorPageResult(LayoutPath, layout));

            var viewStartProvider = CreateViewStartProvider();
            var view = new RazorView(viewEngine.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     viewStartProvider,
                                     page,
                                     isPartial: true);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Mock.Get(viewStartProvider)
                .Verify(v => v.GetViewStartPages(It.IsAny<string>()), Times.Never());
            Assert.Equal(expected, viewContext.Writer.ToString());
        }

        [Fact]
        public async Task RenderAsync_CreatesOutputBuffer()
        {
            // Arrange
            TextWriter actual = null;
            var page = new TestableRazorPage(v =>
            {
                actual = v.Output;
            });
            var view = new RazorView(Mock.Of<IRazorViewEngine>(),
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider(),
                                     page,
                                     isPartial: false);
            var viewContext = CreateViewContext(view);
            var original = viewContext.Writer;

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.IsType<RazorTextWriter>(actual);
            Assert.NotSame(original, actual);
        }

        [Fact]
        public async Task RenderAsync_CopiesBufferedContentToOutput()
        {
            // Arrange
            var page = new TestableRazorPage(v =>
            {
                v.WriteLiteral("Hello world");
            });
            var view = new RazorView(Mock.Of<IRazorViewEngine>(),
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider(),
                                     page,
                                     isPartial: false);
            var viewContext = CreateViewContext(view);
            var original = viewContext.Writer;

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.Equal("Hello world", original.ToString());
        }

        [Fact]
        public async Task RenderAsync_ActivatesPages()
        {
            // Arrange
            var page = new TestableRazorPage(v =>
            {
                v.WriteLiteral("Hello world");
            });
            var activator = new Mock<IRazorPageActivator>();
            activator.Setup(a => a.Activate(page, It.IsAny<ViewContext>()))
                     .Verifiable();
            var view = new RazorView(Mock.Of<IRazorViewEngine>(),
                                     activator.Object,
                                     CreateViewStartProvider(),
                                     page,
                                     isPartial: false);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            activator.Verify();
        }

        [Fact]
        public async Task RenderAsync_ExecutesViewStart()
        {
            // Arrange
            var actualLayoutPath = "";
            var layoutPath = "/Views/_Shared/_Layout.cshtml";
            var viewStart1 = new TestableRazorPage(v =>
            {
                v.Layout = "/fake-layout-path";
            });
            var viewStart2 = new TestableRazorPage(v =>
            {
                v.Layout = layoutPath;
            });
            var page = new TestableRazorPage(v =>
            {
                // This path must have been set as a consequence of running viewStart
                actualLayoutPath = v.Layout;
                // Clear out layout so we don't render it
                v.Layout = null;
            });
            var activator = new Mock<IRazorPageActivator>();
            activator.Setup(a => a.Activate(viewStart1, It.IsAny<ViewContext>()))
                     .Verifiable();
            activator.Setup(a => a.Activate(viewStart2, It.IsAny<ViewContext>()))
                     .Verifiable();
            activator.Setup(a => a.Activate(page, It.IsAny<ViewContext>()))
                     .Verifiable();
            var view = new RazorView(Mock.Of<IRazorViewEngine>(),
                                     activator.Object,
                                     CreateViewStartProvider(viewStart1, viewStart2),
                                     page,
                                     isPartial: false);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            activator.Verify();
        }

        [Fact]
        public async Task RenderAsync_ThrowsIfLayoutPageCannotBeFound()
        {
            // Arrange
            var expected = string.Join(Environment.NewLine,
                                       "The layout view 'Does-Not-Exist-Layout' could not be located. " +
                                       "The following locations were searched:",
                                       "path1",
                                       "path2");

            var layoutPath = "Does-Not-Exist-Layout";
            var page = new TestableRazorPage(v =>
            {
                v.Layout = layoutPath;
            });

            var viewEngine = new Mock<IRazorViewEngine>();
            var activator = new Mock<IRazorPageActivator>();
            var view = new RazorView(viewEngine.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     Mock.Of<IViewStartProvider>(),
                                     page,
                                     isPartial: false);
            var viewContext = CreateViewContext(view);
            viewEngine.Setup(v => v.FindPage(viewContext, layoutPath))
                      .Returns(new RazorPageResult(layoutPath, new[] { "path1", "path2" }))
                      .Verifiable();

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => view.RenderAsync(viewContext));

            // Assert
            Assert.Equal(expected, ex.Message);
            viewEngine.Verify();
        }

        [Fact]
        public async Task RenderAsync_ExecutesLayoutPages()
        {
            // Arrange
            var htmlEncoder = new HtmlEncoder();
            var htmlEncodedNewLine = htmlEncoder.HtmlEncode(Environment.NewLine);
            var expected = "layout-content" +
                           htmlEncodedNewLine +
                           "head-content" +
                           htmlEncodedNewLine +
                           "body-content" +
                           htmlEncodedNewLine +
                           "foot-content";

            var page = new TestableRazorPage(v =>
            {
                v.HtmlEncoder = htmlEncoder;
                v.WriteLiteral("body-content");
                v.Layout = LayoutPath;
                v.DefineSection("head", async writer =>
                {
                    await writer.WriteAsync("head-content");
                });
                v.DefineSection("foot", async writer =>
                {
                    await writer.WriteAsync("foot-content");
                });
            });
            var layout = new TestableRazorPage(v =>
            {
                v.HtmlEncoder = htmlEncoder;
                v.Write("layout-content" + Environment.NewLine);
                v.Write(v.RenderSection("head"));
                v.Write(Environment.NewLine);
                v.RenderBodyPublic();
                v.Write(Environment.NewLine);
                v.Write(v.RenderSection("foot"));
            });
            var activator = new Mock<IRazorPageActivator>();
            activator.Setup(a => a.Activate(page, It.IsAny<ViewContext>()))
                     .Verifiable();
            activator.Setup(a => a.Activate(layout, It.IsAny<ViewContext>()))
                     .Verifiable();
            var viewEngine = new Mock<IRazorViewEngine>();

            var view = new RazorView(viewEngine.Object,
                                     activator.Object,
                                     CreateViewStartProvider(),
                                     page,
                                     isPartial: false);
            var viewContext = CreateViewContext(view);
            viewEngine.Setup(p => p.FindPage(viewContext, LayoutPath))
                       .Returns(new RazorPageResult(LayoutPath, layout))
                       .Verifiable();

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            // Verify the activator was invoked for the primary page and layout page.
            activator.Verify();
            Assert.Equal(expected, viewContext.Writer.ToString());
            viewEngine.Verify();
        }

        [Fact]
        public async Task RenderAsync_ThrowsIfSectionsWereDefinedButNotRendered()
        {
            // Arrange
            var page = new TestableRazorPage(v =>
            {
                v.DefineSection("head", _nullRenderAsyncDelegate);
                v.Layout = LayoutPath;
                v.DefineSection("foot", _nullRenderAsyncDelegate);
            });
            var layout = new TestableRazorPage(v =>
            {
                v.RenderBodyPublic();
            });
            var viewEngine = new Mock<IRazorViewEngine>();
            viewEngine.Setup(p => p.FindPage(It.IsAny<ActionContext>(), LayoutPath))
                       .Returns(new RazorPageResult(LayoutPath, layout));

            var view = new RazorView(viewEngine.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider(),
                                     page,
                                     isPartial: false);
            var viewContext = CreateViewContext(view);

            // Act and Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => view.RenderAsync(viewContext));
            Assert.Equal("The following sections have been defined but have not been rendered: 'head, foot'.", ex.Message);
        }

        [Fact]
        public async Task RenderAsync_ThrowsIfBodyWasNotRendered()
        {
            // Arrange
            var page = new TestableRazorPage(v =>
            {
                v.Layout = LayoutPath;
            });
            var layout = new TestableRazorPage(v =>
            {
            });
            var viewEngine = new Mock<IRazorViewEngine>();
            viewEngine.Setup(p => p.FindPage(It.IsAny<ActionContext>(), LayoutPath))
                       .Returns(new RazorPageResult(LayoutPath, layout));

            var view = new RazorView(viewEngine.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider(),
                                     page,
                                     isPartial: false);
            var viewContext = CreateViewContext(view);

            // Act and Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => view.RenderAsync(viewContext));
            Assert.Equal("RenderBody must be called from a layout page.", ex.Message);
        }

        [Fact]
        public async Task RenderAsync_ExecutesNestedLayoutPages()
        {
            // Arrange
            var htmlEncoder = new HtmlEncoder();
            var htmlEncodedNewLine = htmlEncoder.HtmlEncode(Environment.NewLine);
            var expected = "layout-2" +
                           htmlEncodedNewLine +
                           "bar-content" +
                           Environment.NewLine +
                           "layout-1" +
                           htmlEncodedNewLine +
                           "foo-content" +
                           Environment.NewLine +
                           "body-content";

            var page = new TestableRazorPage(v =>
            {
                v.HtmlEncoder = htmlEncoder;
                v.DefineSection("foo", async writer =>
                {
                    await writer.WriteLineAsync("foo-content");
                });
                v.Layout = "~/Shared/Layout1.cshtml";
                v.WriteLiteral("body-content");
            });
            var layout1 = new TestableRazorPage(v =>
            {
                v.HtmlEncoder = htmlEncoder;
                v.Write("layout-1" + Environment.NewLine);
                v.Write(v.RenderSection("foo"));
                v.DefineSection("bar", writer => writer.WriteLineAsync("bar-content"));
                v.RenderBodyPublic();
                v.Layout = "~/Shared/Layout2.cshtml";
            });
            var layout2 = new TestableRazorPage(v =>
            {
                v.HtmlEncoder = htmlEncoder;
                v.Write("layout-2" + Environment.NewLine);
                v.Write(v.RenderSection("bar"));
                v.RenderBodyPublic();
            });
            var viewEngine = new Mock<IRazorViewEngine>();
            viewEngine.Setup(p => p.FindPage(It.IsAny<ActionContext>(), "~/Shared/Layout1.cshtml"))
                       .Returns(new RazorPageResult("~/Shared/Layout1.cshtml", layout1));
            viewEngine.Setup(p => p.FindPage(It.IsAny<ActionContext>(), "~/Shared/Layout2.cshtml"))
                       .Returns(new RazorPageResult("~/Shared/Layout2.cshtml", layout2));

            var view = new RazorView(viewEngine.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider(),
                                     page,
                                     isPartial: false);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.Equal(expected, viewContext.Writer.ToString());
        }

        [Fact]
        public async Task RenderAsync_DoesNotCopyContentOnceRazorTextWriterIsNoLongerBuffering()
        {
            // Arrange
            var htmlEncoder = new HtmlEncoder();
            var expected = "layout-1" +
                           htmlEncoder.HtmlEncode(Environment.NewLine) +
                           "body content" +
                           Environment.NewLine +
                           "section-content-1" +
                           Environment.NewLine +
                           "section-content-2";

            var page = new TestableRazorPage(v =>
            {
                v.HtmlEncoder = htmlEncoder;
                v.Layout = "layout-1";
                v.WriteLiteral("body content" + Environment.NewLine);
                v.DefineSection("foo", async _ =>
                {
                    v.WriteLiteral("section-content-1" + Environment.NewLine);
                    await v.FlushAsync();
                    v.WriteLiteral("section-content-2");
                });
            });

            var layout1 = new TestableRazorPage(v =>
            {
                v.HtmlEncoder = htmlEncoder;
                v.Write("layout-1" + Environment.NewLine);
                v.RenderBodyPublic();
                v.Write(v.RenderSection("foo"));
            });

            var viewEngine = new Mock<IRazorViewEngine>();
            viewEngine.Setup(p => p.FindPage(It.IsAny<ActionContext>(), "layout-1"))
                       .Returns(new RazorPageResult("layout-1", layout1));

            var view = new RazorView(viewEngine.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider(),
                                     page,
                                     isPartial: false);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.Equal(expected, viewContext.Writer.ToString());
        }

        [Fact]
        public async Task FlushAsync_DoesNotThrowWhenInvokedInsideOfASection()
        {
            // Arrange
            var htmlEncoder = new HtmlEncoder();
            var expected = "layout-1" +
                           htmlEncoder.HtmlEncode(Environment.NewLine) +
                           "section-content-1" +
                           Environment.NewLine +
                           "section-content-2";

            var page = new TestableRazorPage(v =>
           {
               v.HtmlEncoder = htmlEncoder;
               v.Layout = "layout-1";
               v.DefineSection("foo", async _ =>
               {
                   v.WriteLiteral("section-content-1" + Environment.NewLine);
                   await v.FlushAsync();
                   v.WriteLiteral("section-content-2");
               });
           });

            var layout1 = new TestableRazorPage(v =>
            {
                v.HtmlEncoder = htmlEncoder;
                v.Write("layout-1" + Environment.NewLine);
                v.RenderBodyPublic();
                v.Write(v.RenderSection("foo"));
            });

            var viewEngine = new Mock<IRazorViewEngine>();
            viewEngine.Setup(p => p.FindPage(It.IsAny<ActionContext>(), "layout-1"))
                       .Returns(new RazorPageResult("layout-1", layout1));

            var view = new RazorView(viewEngine.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider(),
                                     page,
                                     isPartial: false);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.Equal(expected, viewContext.Writer.ToString());
        }

        [Fact]
        public async Task RenderAsync_ThrowsIfLayoutIsSpecifiedWhenNotBuffered()
        {
            // Arrange
            var expected = @"A layout page cannot be rendered after 'FlushAsync' has been invoked.";
            var page = new TestableRazorPage(v =>
            {
                v.WriteLiteral("before-flush" + Environment.NewLine);
                v.FlushAsync().Wait();
                v.Layout = "test-layout";
                v.WriteLiteral("after-flush");
            });

            var view = new RazorView(Mock.Of<IRazorViewEngine>(),
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider(),
                                     page,
                                     isPartial: false);
            var viewContext = CreateViewContext(view);

            // Act and Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => view.RenderAsync(viewContext));
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public async Task RenderAsync_ThrowsIfFlushWasInvokedInsideRenderedSectionAndLayoutWasSet()
        {
            // Arrange
            var expected = @"A layout page cannot be rendered after 'FlushAsync' has been invoked.";
            var page = new TestableRazorPage(v =>
            {
                v.HtmlEncoder = new HtmlEncoder();
                v.DefineSection("foo", async writer =>
                {
                    writer.WriteLine("foo-content");
                    await v.FlushAsync();
                });
                v.Layout = "~/Shared/Layout1.cshtml";
                v.WriteLiteral("body-content");
            });
            var layoutPage = new TestableRazorPage(v =>
            {
                v.HtmlEncoder = new HtmlEncoder();
                v.Write("layout-1" + Environment.NewLine);
                v.Write(v.RenderSection("foo"));
                v.DefineSection("bar", writer => writer.WriteLineAsync("bar-content"));
                v.RenderBodyPublic();
                v.Layout = "~/Shared/Layout2.cshtml";
            });
            var viewEngine = new Mock<IRazorViewEngine>();
            var layoutPath = "~/Shared/Layout1.cshtml";
            viewEngine.Setup(p => p.FindPage(It.IsAny<ActionContext>(), layoutPath))
                       .Returns(new RazorPageResult(layoutPath, layoutPage));

            var view = new RazorView(viewEngine.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider(),
                                     page,
                                     isPartial: false);
            var viewContext = CreateViewContext(view);

            // Act and Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => view.RenderAsync(viewContext));
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public async Task RenderAsync_UsesPageExecutionFeatureFromRequest_ToWrapWriter()
        {
            // Arrange
            var pageWriter = CreateBufferedWriter();
            var layoutWriter = CreateBufferedWriter();

            var layoutExecuted = false;
            var count = -1;
            var feature = new Mock<IPageExecutionListenerFeature>(MockBehavior.Strict);
            feature.Setup(f => f.DecorateWriter(It.IsAny<RazorTextWriter>()))
                   .Returns(() =>
                   {
                       count++;
                       if (count == 0)
                       {
                           return pageWriter;
                       }
                       else if (count == 1)
                       {
                           return layoutWriter;
                       }
                       throw new Exception();
                   })
                   .Verifiable();

            var pageContext = Mock.Of<IPageExecutionContext>();
            feature.Setup(f => f.GetContext("/MyPage.cshtml", pageWriter))
                    .Returns(pageContext)
                    .Verifiable();

            var layoutContext = Mock.Of<IPageExecutionContext>();
            feature.Setup(f => f.GetContext("/Layout.cshtml", layoutWriter))
                    .Returns(layoutContext)
                    .Verifiable();

            var page = new TestableRazorPage(v =>
            {
                v.HtmlEncoder = new HtmlEncoder();
                v.Layout = "Layout";
                Assert.Same(pageWriter, v.Output);
                Assert.Same(pageContext, v.PageExecutionContext);
            });
            page.Path = "/MyPage.cshtml";

            var layout = new TestableRazorPage(v =>
            {
                v.HtmlEncoder = new HtmlEncoder();
                Assert.Same(layoutWriter, v.Output);
                Assert.Same(layoutContext, v.PageExecutionContext);
                v.RenderBodyPublic();

                layoutExecuted = true;
            });
            layout.Path = "/Layout.cshtml";

            var viewEngine = new Mock<IRazorViewEngine>();
            viewEngine.Setup(p => p.FindPage(It.IsAny<ActionContext>(), "Layout"))
                       .Returns(new RazorPageResult("Layout", layout));
            var viewStartProvider = new Mock<IViewStartProvider>();
            viewStartProvider.Setup(v => v.GetViewStartPages(It.IsAny<string>()))
                             .Returns(Enumerable.Empty<IRazorPage>())
                             .Verifiable();
            var view = new RazorView(viewEngine.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     viewStartProvider.Object,
                                     page,
                                     isPartial: false);
            var viewContext = CreateViewContext(view);
            viewContext.HttpContext.SetFeature<IPageExecutionListenerFeature>(feature.Object);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            feature.Verify();
            viewStartProvider.Verify();
            Assert.True(layoutExecuted);
        }

        [Fact]
        public async Task RenderAsync_UsesPageExecutionFeatureFromRequest_ToGetExecutionContext()
        {
            // Arrange
            var writer = new StringWriter();
            var executed = false;
            var feature = new Mock<IPageExecutionListenerFeature>(MockBehavior.Strict);

            var pageContext = Mock.Of<IPageExecutionContext>();
            feature.Setup(f => f.GetContext("/MyPartialPage.cshtml", It.IsAny<RazorTextWriter>()))
                    .Returns(pageContext)
                    .Verifiable();

            feature.Setup(f => f.DecorateWriter(It.IsAny<RazorTextWriter>()))
                   .Returns((RazorTextWriter r) => r)
                   .Verifiable();

            var page = new TestableRazorPage(v =>
            {
                v.HtmlEncoder = new HtmlEncoder();
                Assert.IsType<RazorTextWriter>(v.Output);
                Assert.Same(pageContext, v.PageExecutionContext);
                executed = true;

                v.Write("Hello world");
            });
            page.Path = "/MyPartialPage.cshtml";

            var view = new RazorView(Mock.Of<IRazorViewEngine>(),
                                     Mock.Of<IRazorPageActivator>(),
                                     Mock.Of<IViewStartProvider>(),
                                     page,
                                     isPartial: true);
            var viewContext = CreateViewContext(view);
            viewContext.Writer = writer;
            viewContext.HttpContext.SetFeature<IPageExecutionListenerFeature>(feature.Object);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            feature.Verify();
            Assert.True(executed);
            Assert.Equal("Hello world", viewContext.Writer.ToString());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RenderAsync_DoesNotSetExecutionContextWhenListenerIsNotRegistered(bool isPartial)
        {
            // Arrange
            var executed = false;
            var page = new TestableRazorPage(v =>
            {
                Assert.Null(v.PageExecutionContext);
                executed = true;
            });

            var view = new RazorView(Mock.Of<IRazorViewEngine>(),
                                     Mock.Of<IRazorPageActivator>(),
                                     Mock.Of<IViewStartProvider>(),
                                     page,
                                     isPartial);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.True(executed);
        }

        [Fact]
        public async Task RenderAsync_CopiesLayoutPropertyFromViewStart()
        {
            // Arrange
            var expectedViewStart = "Layout1";
            var expectedPage = "Layout2";
            string actualViewStart = null;
            string actualPage = null;
            var page = new TestableRazorPage(v =>
            {
                actualPage = v.Layout;
                // Clear it out because we don't care about rendering the layout in this test.
                v.Layout = null;
            });
            var viewStart1 = new TestableRazorPage(v =>
            {
                v.Layout = expectedViewStart;
            });
            var viewStart2 = new TestableRazorPage(v =>
            {
                actualViewStart = v.Layout;
                v.Layout = expectedPage;
            });
            var viewEngine = Mock.Of<IRazorViewEngine>();

            var view = new RazorView(viewEngine,
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider(viewStart1, viewStart2),
                                     page,
                                     isPartial: false);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.Equal(expectedViewStart, actualViewStart);
            Assert.Equal(expectedPage, actualPage);
        }

        [Fact]
        public async Task ResettingLayout_InViewStartCausesItToBeResetInPage()
        {
            // Arrange
            var expected = "Layout";
            string actual = null;

            var page = new TestableRazorPage(v =>
            {
                Assert.Null(v.Layout);
            });
            var viewStart1 = new TestableRazorPage(v =>
            {
                v.Layout = expected;
            });
            var viewStart2 = new TestableRazorPage(v =>
            {
                actual = v.Layout;
                v.Layout = null;
            });
            var viewEngine = Mock.Of<IRazorViewEngine>();

            var view = new RazorView(viewEngine,
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider(viewStart1, viewStart2),
                                     page,
                                     isPartial: false);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task IsPartial_IsSetToFalse_ForViewStartPageAndLayoutOfAView()
        {
            // Arrange
            bool? isPartialPage = null;
            bool? isPartialLayout = null;
            bool? isPartialViewStart = null;

            var page = new TestableRazorPage(v =>
            {
                isPartialPage = v.IsPartial;
            });
            var viewStart = new TestableRazorPage(v =>
            {
                v.Layout = "/Layout.cshtml";
                isPartialViewStart = v.IsPartial;
            });
            var layout = new TestableRazorPage(v =>
            {
                isPartialLayout = v.IsPartial;
                v.RenderBodyPublic();
            });
            var viewEngine = new Mock<IRazorViewEngine>();
            viewEngine.Setup(p => p.FindPage(It.IsAny<ActionContext>(), "/Layout.cshtml"))
                       .Returns(new RazorPageResult("Layout", layout));

            var view = new RazorView(viewEngine.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider(viewStart),
                                     page,
                                     isPartial: false);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.False(isPartialPage.Value);
            Assert.False(isPartialLayout.Value);
            Assert.False(isPartialViewStart.Value);
        }

        [Fact]
        public async Task IsPartial_IsSetToTrue_ForPartialView()
        {
            // Arrange
            bool? isPartialPage = null;
            var page = new TestableRazorPage(v =>
            {
                isPartialPage = v.IsPartial;
            });
            var view = new RazorView(Mock.Of<IRazorViewEngine>(),
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider(),
                                     page,
                                     isPartial: true);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.True(isPartialPage.Value);
        }

        private static TextWriter CreateBufferedWriter()
        {
            var mockWriter = new Mock<TextWriter>();
            var bufferedWriter = mockWriter.As<IBufferedTextWriter>();
            bufferedWriter.SetupGet(b => b.IsBuffering)
                          .Returns(true);
            return mockWriter.Object;
        }

        private static ViewContext CreateViewContext(RazorView view)
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, routeData: null, actionDescriptor: null);
            return new ViewContext(
                actionContext,
                view,
                new ViewDataDictionary(new EmptyModelMetadataProvider()),
                Mock.Of<ITempDataDictionary>(),
                new StringWriter());
        }

        private static IViewStartProvider CreateViewStartProvider(params IRazorPage[] viewStartPages)
        {
            viewStartPages = viewStartPages ?? new IRazorPage[0];
            var viewStartProvider = new Mock<IViewStartProvider>();
            viewStartProvider.Setup(v => v.GetViewStartPages(It.IsAny<string>()))
                             .Returns(viewStartPages);

            return viewStartProvider.Object;
        }

        private class TestableRazorPage : RazorPage
        {
            private readonly Action<TestableRazorPage> _executeAction;

            public TestableRazorPage(Action<TestableRazorPage> executeAction)
            {
                _executeAction = executeAction;
            }

            public void RenderBodyPublic()
            {
                Write(RenderBody());
            }

            public override Task ExecuteAsync()
            {
                _executeAction(this);
                return Task.FromResult(0);
            }
        }
    }
}