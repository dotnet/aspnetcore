using System;
using System.IO;
using System.Threading.Tasks;
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
            Exception ex = null;
            var view = CreateView(v =>
            {
                v.DefineSection("foo", new HelperResult(action: null));

                ex = Assert.Throws<InvalidOperationException>(
                        () => v.DefineSection("foo", new HelperResult(action: null)));
            });
            var viewContext = CreateViewContext(layoutView: null);

            // Act
            await view.RenderAsync(viewContext);

            // Assert
            Assert.Equal("Section 'foo' is already defined.", ex.Message);
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
            Exception ex = null;
            var view = CreateView(v =>
            {
                v.DefineSection("baz", expected);
                v.Layout = LayoutPath;
            });
            var layoutView = CreateView(v =>
            {
                ex = Assert.Throws<InvalidOperationException>(
                        () => v.RenderSection("bar"));
            });
            var viewContext = CreateViewContext(layoutView);

            // Act
            await view.RenderAsync(viewContext);

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
                v.DefineSection("foo", new HelperResult(writer => { }));
                v.Layout = LayoutPath;
            });
            var layoutView = CreateView(v =>
            {
                actual = v.IsSectionDefined("foo");
            });

            // Act
            await view.RenderAsync(CreateViewContext(layoutView));

            // Assert
            Assert.Equal(true, actual);
        }

        public static RazorView CreateView(Action<RazorView> executeAction)
        {
            var view = new Mock<RazorView> { CallBase = true };
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
            return new ViewContext(serviceProvider.Object, httpContext: null, viewEngineContext: null)
            {
                Writer = new StringWriter()
            };
        }
    }
}