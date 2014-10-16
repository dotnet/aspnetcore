// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.PageExecutionInstrumentation;
using Microsoft.AspNet.PipelineCore;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorViewTest
    {
        private const string LayoutPath = "~/Shared/_Layout.cshtml";
        private readonly RenderAsyncDelegate _nullRenderAsyncDelegate = async writer => { };

        [Fact]
        public async Task RenderAsync_ThrowsIfContextualizeHasNotBeenInvoked()
        {
            // Arrange
            var page = new TestableRazorPage(v => { });
            var view = new RazorView(Mock.Of<IRazorPageFactory>(),
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider());
            var viewContext = CreateViewContext(view);
            var expected = viewContext.Writer;

            // Act and Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => view.RenderAsync(viewContext));
            Assert.Equal("The 'Contextualize' method must be called before 'RenderAsync' can be invoked.",
                         ex.Message);
        }

        [Fact]
        public async Task RenderAsync_AsPartial_DoesNotBufferOutput()
        {
            // Arrange
            TextWriter actual = null;
            var page = new TestableRazorPage(v =>
            {
                actual = v.Output;
                v.Write("Hello world");
            });
            var view = new RazorView(Mock.Of<IRazorPageFactory>(),
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider());
            view.Contextualize(page, isPartial: true);
            var viewContext = CreateViewContext(view);
            var expected = viewContext.Writer;

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.Same(expected, actual);
            Assert.Equal("Hello world", viewContext.Writer.ToString());
        }

        [Fact]
        public async Task RenderAsync_AsPartial_ActivatesViews_WithThePassedInViewContext()
        {
            // Arrange
            var viewData = new ViewDataDictionary(Mock.Of<IModelMetadataProvider>());
            var page = new TestableRazorPage(v =>
            {
                // viewData is assigned to ViewContext by the activator
                Assert.Same(viewData, v.ViewContext.ViewData);
            });
            var activator = new Mock<IRazorPageActivator>();

            var view = new RazorView(Mock.Of<IRazorPageFactory>(),
                                     activator.Object,
                                     CreateViewStartProvider());
            view.Contextualize(page, isPartial: true);
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
        public async Task RenderAsync_AsPartial_ActivatesViews()
        {
            // Arrange
            var page = new TestableRazorPage(v => { });
            var activator = new Mock<IRazorPageActivator>();
            activator.Setup(a => a.Activate(page, It.IsAny<ViewContext>()))
                     .Verifiable();
            var view = new RazorView(Mock.Of<IRazorPageFactory>(),
                                     activator.Object,
                                     CreateViewStartProvider());
            view.Contextualize(page, isPartial: true);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            activator.Verify();
        }

        [Fact]
        public async Task RenderAsync_AsPartial_DoesNotExecuteLayoutOrViewStartPages()
        {
            var page = new TestableRazorPage(v =>
            {
                v.Layout = LayoutPath;
            });
            var pageFactory = new Mock<IRazorPageFactory>();
            var viewStartProvider = CreateViewStartProvider();
            var view = new RazorView(pageFactory.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     viewStartProvider);
            view.Contextualize(page, isPartial: true);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            pageFactory.Verify(v => v.CreateInstance(It.IsAny<string>()), Times.Never());
            Mock.Get(viewStartProvider)
                .Verify(v => v.GetViewStartPages(It.IsAny<string>()), Times.Never());
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
            var view = new RazorView(Mock.Of<IRazorPageFactory>(),
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider());
            view.Contextualize(page, isPartial: false);
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
            var view = new RazorView(Mock.Of<IRazorPageFactory>(),
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider());
            view.Contextualize(page, isPartial: false);
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
            var view = new RazorView(Mock.Of<IRazorPageFactory>(),
                                     activator.Object,
                                     CreateViewStartProvider());
            view.Contextualize(page, isPartial: false);

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
            var view = new RazorView(Mock.Of<IRazorPageFactory>(),
                                     activator.Object,
                                     CreateViewStartProvider(viewStart1, viewStart2));
            view.Contextualize(page, isPartial: false);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            activator.Verify();
        }

        [Fact]
        public async Task RenderAsync_ExecutesLayoutPages()
        {
            // Arrange
            var expected =
@"layout-content
head-content
body-content
foot-content";

            var page = new TestableRazorPage(v =>
            {
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
            var pageFactory = new Mock<IRazorPageFactory>();
            pageFactory.Setup(p => p.CreateInstance(LayoutPath))
                       .Returns(layout);

            var view = new RazorView(pageFactory.Object,
                                     activator.Object,
                                     CreateViewStartProvider());
            view.Contextualize(page, isPartial: false);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            // Verify the activator was invoked for the primary page and layout page.
            activator.Verify();
            Assert.Equal(expected, viewContext.Writer.ToString());
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
            var pageFactory = new Mock<IRazorPageFactory>();
            pageFactory.Setup(p => p.CreateInstance(LayoutPath))
                       .Returns(layout);

            var view = new RazorView(pageFactory.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider());
            view.Contextualize(page, isPartial: false);
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
            var pageFactory = new Mock<IRazorPageFactory>();
            pageFactory.Setup(p => p.CreateInstance(LayoutPath))
                       .Returns(layout);

            var view = new RazorView(pageFactory.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider());
            view.Contextualize(page, isPartial: false);
            var viewContext = CreateViewContext(view);

            // Act and Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => view.RenderAsync(viewContext));
            Assert.Equal("RenderBody must be called from a layout page.", ex.Message);
        }

        [Fact]
        public async Task RenderAsync_ExecutesNestedLayoutPages()
        {
            // Arrange
            var expected =
@"layout-2
bar-content
layout-1
foo-content
body-content";

            var page = new TestableRazorPage(v =>
            {
                v.DefineSection("foo", async writer =>
                {
                    await writer.WriteLineAsync("foo-content");
                });
                v.Layout = "~/Shared/Layout1.cshtml";
                v.WriteLiteral("body-content");
            });
            var layout1 = new TestableRazorPage(v =>
            {
                v.Write("layout-1" + Environment.NewLine);
                v.Write(v.RenderSection("foo"));
                v.DefineSection("bar", writer => writer.WriteLineAsync("bar-content"));
                v.RenderBodyPublic();
                v.Layout = "~/Shared/Layout2.cshtml";
            });
            var layout2 = new TestableRazorPage(v =>
            {
                v.Write("layout-2" + Environment.NewLine);
                v.Write(v.RenderSection("bar"));
                v.RenderBodyPublic();
            });
            var pageFactory = new Mock<IRazorPageFactory>();
            pageFactory.Setup(p => p.CreateInstance("~/Shared/Layout1.cshtml"))
                       .Returns(layout1);
            pageFactory.Setup(p => p.CreateInstance("~/Shared/Layout2.cshtml"))
                       .Returns(layout2);

            var view = new RazorView(pageFactory.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider());
            view.Contextualize(page, isPartial: false);
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
            var expected =
@"layout-1
body content
section-content-1
section-content-2";

            var page = new TestableRazorPage(v =>
            {
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
                v.Write("layout-1" + Environment.NewLine);
                v.RenderBodyPublic();
                v.Write(v.RenderSection("foo"));
            });

            var pageFactory = new Mock<IRazorPageFactory>();
            pageFactory.Setup(p => p.CreateInstance("layout-1"))
                       .Returns(layout1);

            var view = new RazorView(pageFactory.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider());
            view.Contextualize(page, isPartial: false);
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
            var expected =
@"layout-1
section-content-1
section-content-2";

            var page = new TestableRazorPage(v =>
           {
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
                v.Write("layout-1" + Environment.NewLine);
                v.RenderBodyPublic();
                v.Write(v.RenderSection("foo"));
            });

            var pageFactory = new Mock<IRazorPageFactory>();
            pageFactory.Setup(p => p.CreateInstance("layout-1"))
                       .Returns(layout1);

            var view = new RazorView(pageFactory.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider());
            view.Contextualize(page, isPartial: false);
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

            var view = new RazorView(Mock.Of<IRazorPageFactory>(),
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider());
            view.Contextualize(page, isPartial: false);
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
                v.DefineSection("foo", async writer =>
                {
                    writer.WriteLine("foo-content");
                    await v.FlushAsync();
                });
                v.Layout = "~/Shared/Layout1.cshtml";
                v.WriteLiteral("body-content");
            });
            var layout1 = new TestableRazorPage(v =>
            {
                v.Write("layout-1" + Environment.NewLine);
                v.Write(v.RenderSection("foo"));
                v.DefineSection("bar", writer => writer.WriteLineAsync("bar-content"));
                v.RenderBodyPublic();
                v.Layout = "~/Shared/Layout2.cshtml";
            });
            var pageFactory = new Mock<IRazorPageFactory>();
            pageFactory.Setup(p => p.CreateInstance("~/Shared/Layout1.cshtml"))
                       .Returns(layout1);

            var view = new RazorView(pageFactory.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider());
            view.Contextualize(page, isPartial: false);
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
                v.Layout = "/Layout.cshtml";
                Assert.Same(pageWriter, v.Output);
                Assert.Same(pageContext, v.PageExecutionContext);
            });
            page.Path = "/MyPage.cshtml";

            var layout = new TestableRazorPage(v =>
            {
                Assert.Same(layoutWriter, v.Output);
                Assert.Same(layoutContext, v.PageExecutionContext);
                v.RenderBodyPublic();

                layoutExecuted = true;
            });
            layout.Path = "/Layout.cshtml";

            var pageFactory = new Mock<IRazorPageFactory>();
            pageFactory.Setup(p => p.CreateInstance("/Layout.cshtml"))
                       .Returns(layout);
            var viewStartProvider = new Mock<IViewStartProvider>();
            viewStartProvider.Setup(v => v.GetViewStartPages(It.IsAny<string>()))
                             .Returns(Enumerable.Empty<IRazorPage>())
                             .Verifiable();
            var view = new RazorView(pageFactory.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     viewStartProvider.Object);
            view.Contextualize(page, isPartial: false);
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
                Assert.IsType<RazorTextWriter>(v.Output);
                Assert.Same(pageContext, v.PageExecutionContext);
                executed = true;

                v.Write("Hello world");
            });
            page.Path = "/MyPartialPage.cshtml";

            var view = new RazorView(Mock.Of<IRazorPageFactory>(),
                                     Mock.Of<IRazorPageActivator>(),
                                     Mock.Of<IViewStartProvider>());
            view.Contextualize(page, isPartial: true);
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

            var view = new RazorView(Mock.Of<IRazorPageFactory>(),
                                     Mock.Of<IRazorPageActivator>(),
                                     Mock.Of<IViewStartProvider>());
            view.Contextualize(page, isPartial);
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
            var pageFactory = Mock.Of<IRazorPageFactory>();

            var view = new RazorView(pageFactory,
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider(viewStart1, viewStart2));
            view.Contextualize(page, isPartial: false);
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
            var pageFactory = Mock.Of<IRazorPageFactory>();

            var view = new RazorView(pageFactory,
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider(viewStart1, viewStart2));
            view.Contextualize(page, isPartial: false);
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
            var pageFactory = new Mock<IRazorPageFactory>();
            pageFactory.Setup(p => p.CreateInstance("/Layout.cshtml"))
                       .Returns(layout);

            var view = new RazorView(pageFactory.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider(viewStart));
            view.Contextualize(page, isPartial: false);
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
            var view = new RazorView(Mock.Of<IRazorPageFactory>(),
                                     Mock.Of<IRazorPageActivator>(),
                                     CreateViewStartProvider());
            view.Contextualize(page, isPartial: true);
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
                new ViewDataDictionary(Mock.Of<IModelMetadataProvider>()),
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