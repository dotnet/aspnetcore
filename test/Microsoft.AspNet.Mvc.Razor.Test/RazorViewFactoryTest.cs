// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorViewFactoryTest
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetView_SetsIsPartial(bool isPartial)
        {
            // Arrange
            var factory = new RazorViewFactory(
                Mock.Of<IRazorPageActivator>(),
                Mock.Of<IViewStartProvider>(),
                new HtmlTestEncoder());
            var page = Mock.Of<IRazorPage>();
            var viewEngine = Mock.Of<IRazorViewEngine>();

            // Act
            var view = factory.GetView(viewEngine, page, isPartial);

            // Assert
            var razorView = Assert.IsType<RazorView>(view);
            Assert.Same(page, razorView.RazorPage);
            Assert.Equal(razorView.IsPartial, isPartial);
        }

        [Fact]
        public void GetView_SetsRazorPage()
        {
            // Arrange
            var factory = new RazorViewFactory(
                Mock.Of<IRazorPageActivator>(),
                Mock.Of<IViewStartProvider>(),
                new HtmlTestEncoder());

            var page = Mock.Of<IRazorPage>();
            var viewEngine = Mock.Of<IRazorViewEngine>();

            // Act
            var view = factory.GetView(viewEngine, page, isPartial: false);

            // Assert
            Assert.NotNull(view);
            var razorView = Assert.IsType<RazorView>(view);
            Assert.Same(razorView.RazorPage, page);
        }
    }
}