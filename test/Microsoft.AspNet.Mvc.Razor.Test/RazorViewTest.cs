// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorViewTest
    {
        private const string LayoutPath = "~/Shared/_Layout.cshtml";

        [Fact]
        public async Task RenderAsync_WithoutHierarchy_DoesNotCreateOutputBuffer()
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
                                     page,
                                     executeViewHierarchy: false);
            var viewContext = CreateViewContext(view);
            var expected = viewContext.Writer;

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.Same(expected, actual);
            Assert.Equal("Hello world", expected.ToString());
        }

        [Fact]
        public async Task RenderAsync_WithoutHierarchy_ActivatesViews_WithACopyOfViewContext()
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
                                     page,
                                     executeViewHierarchy: false);
            var viewContext = CreateViewContext(view);
            var expectedViewData = viewContext.ViewData;
            var expectedWriter = viewContext.Writer;
            activator.Setup(a => a.Activate(page, It.IsAny<ViewContext>()))
                     .Callback((IRazorPage p, ViewContext c) =>
                     {
                         Assert.NotSame(c, viewContext);
                         c.ViewData = viewData;
                     })
                     .Verifiable();

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            activator.Verify();
            Assert.Same(expectedViewData, viewContext.ViewData);
            Assert.Same(expectedWriter, viewContext.Writer);
        }

        [Fact]
        public async Task RenderAsync_WithoutHierarchy_ActivatesViews()
        {
            // Arrange
            var page = new TestableRazorPage(v => { });
            var activator = new Mock<IRazorPageActivator>();
            activator.Setup(a => a.Activate(page, It.IsAny<ViewContext>()))
                     .Verifiable();
            var view = new RazorView(Mock.Of<IRazorPageFactory>(),
                                     activator.Object,
                                     page,
                                     executeViewHierarchy: false);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            activator.Verify();
        }

        [Fact]
        public async Task RenderAsync_WithoutHierarchy_DoesNotExecuteLayoutPages()
        {
            var page = new TestableRazorPage(v =>
            {
                v.Layout = LayoutPath;
            });
            var pageFactory = new Mock<IRazorPageFactory>();
            var view = new RazorView(pageFactory.Object,
                                     Mock.Of<IRazorPageActivator>(),
                                     page,
                                     executeViewHierarchy: false);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            pageFactory.Verify(v => v.CreateInstance(It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public async Task RenderAsync_WithHierarchy_CreatesOutputBuffer()
        {
            // Arrange
            TextWriter actual = null;
            var page = new TestableRazorPage(v =>
            {
                actual = v.Output;
            });
            var view = new RazorView(Mock.Of<IRazorPageFactory>(),
                                     Mock.Of<IRazorPageActivator>(),
                                     page,
                                     executeViewHierarchy: true);
            var viewContext = CreateViewContext(view);
            var original = viewContext.Writer;

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.IsType<StringWriter>(actual);
            Assert.NotSame(original, actual);
        }

        [Fact]
        public async Task RenderAsync_WithHierarchy_CopiesBufferedContentToOutput()
        {
            // Arrange
            var page = new TestableRazorPage(v =>
            {
                v.WriteLiteral("Hello world");
            });
            var view = new RazorView(Mock.Of<IRazorPageFactory>(),
                                     Mock.Of<IRazorPageActivator>(),
                                     page,
                                     executeViewHierarchy: true);
            var viewContext = CreateViewContext(view);
            var original = viewContext.Writer;

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.Equal("Hello world", original.ToString());
        }

        [Fact]
        public async Task RenderAsync_WithHierarchy_ActivatesPages()
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
                                     page,
                                     executeViewHierarchy: true);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            activator.Verify();
        }

        [Fact]
        public async Task RenderAsync_WithHierarchy_ExecutesLayoutPages()
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
                v.DefineSection("head", new HelperResult(writer =>
                {
                    writer.Write("head-content");
                }));
                v.DefineSection("foot", new HelperResult(writer =>
                {
                    writer.Write("foot-content");
                }));
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
                                     page,
                                     executeViewHierarchy: true);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            // Verify the activator was invoked for the primary page and layout page.
            activator.Verify();
            Assert.Equal(expected, viewContext.Writer.ToString());
        }

        [Fact]
        public async Task RenderAsync_WithHierarchy_ThrowsIfSectionsWereDefinedButNotRendered()
        {
            // Arrange
            var page = new TestableRazorPage(v =>
            {
                v.DefineSection("head", new HelperResult(writer => { }));
                v.Layout = LayoutPath;
                v.DefineSection("foot", new HelperResult(writer => { }));
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
                                     page,
                                     executeViewHierarchy: true);
            var viewContext = CreateViewContext(view);

            // Act and Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => view.RenderAsync(viewContext));
            Assert.Equal("The following sections have been defined but have not been rendered: 'head, foot'.", ex.Message);
        }

        [Fact]
        public async Task RenderAsync_WithHierarchy_ThrowsIfBodyWasNotRendered()
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
                                     page,
                                     executeViewHierarchy: true);
            var viewContext = CreateViewContext(view);

            // Act and Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => view.RenderAsync(viewContext));
            Assert.Equal("RenderBody must be called from a layout page.", ex.Message);
        }

        [Fact]
        public async Task RenderAsync_WithHierarchy_ExecutesNestedLayoutPages()
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
                v.DefineSection("foo", new HelperResult(writer =>
                {
                    writer.WriteLine("foo-content");
                }));
                v.Layout = "~/Shared/Layout1.cshtml";
                v.WriteLiteral("body-content");
            });
            var layout1 = new TestableRazorPage(v =>
            {
                v.Write("layout-1" + Environment.NewLine);
                v.Write(v.RenderSection("foo"));
                v.DefineSection("bar", new HelperResult(writer =>
                {
                    writer.WriteLine("bar-content");
                }));
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
                                     page,
                                     executeViewHierarchy: true);
            var viewContext = CreateViewContext(view);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.Equal(expected, viewContext.Writer.ToString());
        }

        private static ViewContext CreateViewContext(RazorView view)
        {
            var httpContext = new Mock<HttpContext>();
            var actionContext = new ActionContext(httpContext.Object, routeData: null, actionDescriptor: null);
            return new ViewContext(
                actionContext,
                view,
                new ViewDataDictionary(Mock.Of<IModelMetadataProvider>()),
                new StringWriter());
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