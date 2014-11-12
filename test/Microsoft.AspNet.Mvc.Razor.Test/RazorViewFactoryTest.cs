// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.PipelineCore;
using Moq;

namespace Microsoft.AspNet.Mvc.Razor.Test
{
    public class RazorViewFactoryTest
    {

        [Fact]
        public void GetView_SetsIsPartial()
        {
            // Arrange
            var factory = new RazorViewFactory(
                Mock.Of<IRazorPageFactory>(),
                Mock.Of<IRazorPageActivator>(),
                Mock.Of<IViewStartProvider>());

            // Act 
            var viewPartial = factory.GetView(Mock.Of<IRazorPage>(), isPartial: true) as RazorView;
            var view = factory.GetView(Mock.Of<IRazorPage>(), isPartial: false) as RazorView;

            // Assert  
            Assert.NotNull(viewPartial);
            Assert.True(viewPartial.IsPartial);
            Assert.NotNull(view);
            Assert.True(!view.IsPartial);
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

        [Fact]
        public void GetView_ReturnsRazorView()
        {
            // Arrange
            var factory = new RazorViewFactory(
                Mock.Of<IRazorPageFactory>(),
                Mock.Of<IRazorPageActivator>(),
                Mock.Of<IViewStartProvider>());

            // Act 
            var view = factory.GetView(Mock.Of<IRazorPage>(), isPartial: true);

            // Assert  
            Assert.IsType<RazorView>(view);
        }

    }
}