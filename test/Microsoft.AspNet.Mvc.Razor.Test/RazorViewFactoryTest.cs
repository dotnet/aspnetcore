// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Test
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
                Mock.Of<IRazorPageFactory>(),
                Mock.Of<IRazorPageActivator>(),
                Mock.Of<IViewStartProvider>());

            // Act 
            var view = factory.GetView(Mock.Of<IRazorPage>(), isPartial);

            // Assert  
            var razorView = Assert.IsType<RazorView>(view);
            Assert.Equal(razorView.IsPartial, isPartial);
        }

        [Fact]
        public void GetView_SetsRazorPage()
        {
            // Arrange
            var factory = new RazorViewFactory(
                Mock.Of<IRazorPageFactory>(),
                Mock.Of<IRazorPageActivator>(),
                Mock.Of<IViewStartProvider>());

            var page = Mock.Of<IRazorPage>();

            // Act 
            var view = factory.GetView(page, isPartial: false) as RazorView;

            // Assert  
            Assert.NotNull(view);
            Assert.Same(view.RazorPage, page);
        }
    }
}