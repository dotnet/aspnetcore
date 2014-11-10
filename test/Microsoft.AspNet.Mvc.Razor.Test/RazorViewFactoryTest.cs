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
        public void GetView_ReturnsRazorView()
        {
            // Arrange
            var factory = new RazorViewFactory(
                Mock.Of<IRazorPageFactory>(),
                Mock.Of<IRazorPageActivator>(),
                Mock.Of<IViewStartProvider>());

            // Act 
            var view = factory.GetView(Mock.Of<IRazorPage>(), true);

            // Assert  
            Assert.IsType<RazorView>(view);
        }

        [Fact]
        public async Task RenderAsync_ContextualizeMustBeInvoked()
        {
            // Arrange
            var page = new TestableRazorPage(v => { });

            var factory = new RazorViewFactory(
                Mock.Of<IRazorPageFactory>(),
                Mock.Of<IRazorPageActivator>(),
                CreateViewStartProvider());

            // Act 
            var view = factory.GetView(page, true);

            // Assert  
            var viewContext = CreateViewContext(view);
            await Assert.DoesNotThrowAsync(() => view.RenderAsync(viewContext));
        }

        private static IViewStartProvider CreateViewStartProvider()
        {
            var viewStartPages = new IRazorPage[0];
            var viewStartProvider = new Mock<IViewStartProvider>();
            viewStartProvider.Setup(v => v.GetViewStartPages(It.IsAny<string>()))
                             .Returns(viewStartPages);

            return viewStartProvider.Object;
        }

        private static ViewContext CreateViewContext(IView view)
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, routeData: null, actionDescriptor: null);
            return new ViewContext(
                actionContext,
                view,
                new ViewDataDictionary(new EmptyModelMetadataProvider()),
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