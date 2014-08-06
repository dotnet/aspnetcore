// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorPageTest
    {
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
            Mock.Get(page.Context).Setup(c => c.RequestServices)
                                  .Returns(services.Object);

            // Act
            await page.ExecuteAsync();

            // Assert
            var actual = ((StringWriter)page.Output).ToString();
            Assert.Equal(expected, actual);
            helper.Verify();
        }

        private static TestableRazorPage CreatePage(Action<TestableRazorPage> executeAction)
        {
            var view = new Mock<TestableRazorPage> { CallBase = true };
            if (executeAction != null)
            {
                view.Setup(v => v.ExecuteAsync())
                    .Callback(() => executeAction(view.Object))
                    .Returns(Task.FromResult(0));
            }
            view.Object.ViewContext = CreateViewContext();

            return view.Object;
        }

        private static ViewContext CreateViewContext()
        {
            var actionContext = new ActionContext(Mock.Of<HttpContext>(), routeData: null, actionDescriptor: null);
            return new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                null,
                new StringWriter());
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