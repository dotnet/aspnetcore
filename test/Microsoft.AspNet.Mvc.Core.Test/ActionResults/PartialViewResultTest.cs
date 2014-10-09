// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
    public class PartialViewResultTest
    {
        [Fact]
        public async Task PartialViewResult_UsesFindViewOnSpecifiedViewEngineToLocateViews()
        {
            // Arrange
            var viewName = "mypartialview";
            var context = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            var viewEngine = new Mock<IViewEngine>();
            var view = Mock.Of<IView>();

            viewEngine.Setup(e => e.FindPartialView(context, viewName))
                      .Returns(ViewEngineResult.Found(viewName, view))
                      .Verifiable();

            var viewResult = new PartialViewResult
            {
                ViewEngine = viewEngine.Object,
                ViewName = viewName
            };

            // Act
            await viewResult.ExecuteResultAsync(context);

            // Assert
            viewEngine.Verify();
        }
    }
}