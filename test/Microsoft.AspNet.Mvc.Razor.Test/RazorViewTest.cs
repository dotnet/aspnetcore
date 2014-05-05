// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Test
{
    public class RazorViewTest
    {
        private const string LayoutPath = "~/Shared/_Layout.cshtml";

        [Fact]
        public async Task DefineSection_ThrowsIfSectionIsAlreadyDefined()
        {
            // Arrange
            var view = CreateView(v =>
            {
                v.DefineSection("qux", new HelperResult(action: null));
                v.DefineSection("qux", new HelperResult(action: null));
            });
            var viewContext = CreateViewContext(layoutView: null);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                                () => view.RenderAsync(viewContext));

            // Assert
            Assert.Equal("Section 'qux' is already defined.", ex.Message);
        }

        [Fact]
        public async Task RenderSection_RendersSectionFromPreviousPage()
        {
            // Arrange
            var expected = new HelperResult(action: null);
            HelperResult actual = null;
            var view = CreateView(v =>
            {
                v.DefineSection("bar", expected);
                v.Layout = LayoutPath;
            });
            var layoutView = CreateView(v =>
            {
                actual = v.RenderSection("bar");
                v.RenderBodyPublic();
            });
            var viewContext = CreateViewContext(layoutView);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.Same(actual, expected);
        }

        [Fact]
        public async Task RenderSection_ThrowsIfNoPreviousPage()
        {
            // Arrange
            Exception ex = null;
            var view = CreateView(v =>
            {
                ex = Assert.Throws<InvalidOperationException>(() => v.RenderSection("bar"));
            });
            var viewContext = CreateViewContext(layoutView: null);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.Equal("The method 'RenderSection' cannot be invoked by this view.",
                         ex.Message);
        }

        [Fact]
        public async Task RenderSection_ThrowsIfRequiredSectionIsNotFound()
        {
            // Arrange
            var expected = new HelperResult(action: null);
            var view = CreateView(v =>
            {
                v.DefineSection("baz", expected);
                v.Layout = LayoutPath;
            });
            var layoutView = CreateView(v =>
            {
                v.RenderSection("bar");
            });
            var viewContext = CreateViewContext(layoutView);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => view.RenderAsync(viewContext));

            // Assert
            Assert.Equal("Section 'bar' is not defined.", ex.Message);
        }

        [Fact]
        public void IsSectionDefined_ThrowsIfNoPreviousExecutingPage()
        {
            // Arrange
            var view = CreateView(v => { });
            var viewContext = CreateViewContext(layoutView: null);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => view.IsSectionDefined("foo"),
                "The method 'IsSectionDefined' cannot be invoked by this view.");
        }

        [Fact]
        public async Task IsSectionDefined_ReturnsFalseIfSectionNotDefined()
        {
            // Arrange
            bool? actual = null;
            var view = CreateView(v =>
            {
                v.DefineSection("baz", new HelperResult(writer => { }));
                v.Layout = LayoutPath;
            });
            var layoutView = CreateView(v =>
            {
                actual = v.IsSectionDefined("foo");
                v.RenderSection("baz");
                v.RenderBodyPublic();
            });

            // Act
            await view.RenderAsync(CreateViewContext(layoutView));

            // Assert
            Assert.Equal(false, actual);
        }

        [Fact]
        public async Task IsSectionDefined_ReturnsTrueIfSectionDefined()
        {
            // Arrange
            bool? actual = null;
            var view = CreateView(v =>
            {
                v.DefineSection("baz", new HelperResult(writer => { }));
                v.Layout = LayoutPath;
            });
            var layoutView = CreateView(v =>
            {
                actual = v.IsSectionDefined("baz");
                v.RenderSection("baz");
                v.RenderBodyPublic();
            });

            // Act
            await view.RenderAsync(CreateViewContext(layoutView));

            // Assert
            Assert.Equal(true, actual);
        }


        [Fact]
        public async Task RenderSection_ThrowsIfSectionIsRenderedMoreThanOnce()
        {
            // Arrange
            var expected = new HelperResult(action: null);
            var view = CreateView(v =>
            {
                v.DefineSection("header", expected);
                v.Layout = LayoutPath;
            });
            var layoutView = CreateView(v =>
            {
                v.RenderSection("header");
                v.RenderSection("header");
            });
            var viewContext = CreateViewContext(layoutView);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => view.RenderAsync(viewContext));

            // Assert
            Assert.Equal("RenderSection has already been called for the section named 'header'.", ex.Message);
        }

        [Fact]
        public async Task RenderAsync_ThrowsIfDefinedSectionIsNotRendered()
        {
            // Arrange
            var expected = new HelperResult(action: null);
            var view = CreateView(v =>
            {
                v.DefineSection("header", expected);
                v.DefineSection("footer", expected);
                v.DefineSection("sectionA", expected);
                v.Layout = LayoutPath;
            });
            var layoutView = CreateView(v =>
            {
                v.RenderSection("sectionA");
                v.RenderBodyPublic();
            });
            var viewContext = CreateViewContext(layoutView);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => view.RenderAsync(viewContext));

            // Assert
            Assert.Equal("The following sections have been defined but have not been rendered: 'header, footer'.", ex.Message);
        }

        [Fact]
        public async Task RenderAsync_ThrowsIfRenderBodyIsNotCalledFromPage()
        {
            // Arrange
            var expected = new HelperResult(action: null);
            var view = CreateView(v =>
            {
                v.Layout = LayoutPath;
            });
            var layoutView = CreateView(v =>
            {
            });
            var viewContext = CreateViewContext(layoutView);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => view.RenderAsync(viewContext));

            // Assert
            Assert.Equal("RenderBody must be called from a layout page.", ex.Message);
        }

        [Fact]
        public async Task RenderAsync_RendersSectionsAndBody()
        {
            // Arrange
            var expected = @"Layout start
Header section
body content
Footer section
Layout end
";
            var view = CreateView(v =>
            {
                v.Layout = LayoutPath;
                v.WriteLiteral("body content" + Environment.NewLine);

                v.DefineSection("footer", new HelperResult(writer =>
                {
                    writer.WriteLine("Footer section");
                }));

                v.DefineSection("header", new HelperResult(writer =>
                {
                    writer.WriteLine("Header section");
                }));
            });
            var layoutView = CreateView(v =>
            {
                v.WriteLiteral("Layout start" + Environment.NewLine);
                v.Write(v.RenderSection("header"));
                v.Write(v.RenderBodyPublic());
                v.Write(v.RenderSection("footer"));
                v.WriteLiteral("Layout end" + Environment.NewLine);

            });
            var viewContext = CreateViewContext(layoutView);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            var actual = ((StringWriter)viewContext.Writer).ToString();
            Assert.Equal(expected, actual);
        }

        private static TestableRazorView CreateView(Action<TestableRazorView> executeAction)
        {
            var view = new Mock<TestableRazorView> { CallBase = true };
            if (executeAction != null)
            {
                view.Setup(v => v.ExecuteAsync())
                    .Callback(() => executeAction(view.Object))
                    .Returns(Task.FromResult(0));
            }

            return view.Object;
        }

        private static ViewContext CreateViewContext(IView layoutView)
        {
            var viewFactory = new Mock<IVirtualPathViewFactory>();
            viewFactory.Setup(v => v.CreateInstance(LayoutPath))
                       .Returns(layoutView);
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(f => f.GetService(typeof(IVirtualPathViewFactory)))
                            .Returns(viewFactory.Object);

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices).Returns(serviceProvider.Object);
            
            var actionContext = new ActionContext(httpContext.Object, null, null, null);
            return new ViewContext(
                actionContext,
                layoutView,
                null,
                new StringWriter());
        }

        public abstract class TestableRazorView : RazorView
        {
            public HtmlString RenderBodyPublic()
            {
                return base.RenderBody();
            }
        }
    }
}