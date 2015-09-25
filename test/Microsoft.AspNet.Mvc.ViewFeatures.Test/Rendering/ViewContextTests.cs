// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class ViewContextTests
    {
        [Fact]
        public void SettingViewData_AlsoUpdatesViewBag()
        {
            // Arrange
            var originalViewData = new ViewDataDictionary(metadataProvider: new EmptyModelMetadataProvider());
            var context = new ViewContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                view: Mock.Of<IView>(),
                viewData: originalViewData,
                tempData: new TempDataDictionary(new HttpContextAccessor(), Mock.Of<ITempDataProvider>()),
                writer: TextWriter.Null,
                htmlHelperOptions: new HtmlHelperOptions());
            var replacementViewData = new ViewDataDictionary(metadataProvider: new EmptyModelMetadataProvider());

            // Act
            context.ViewBag.Hello = "goodbye";
            context.ViewData = replacementViewData;
            context.ViewBag.Another = "property";

            // Assert
            Assert.NotSame(originalViewData, context.ViewData);
            Assert.Same(replacementViewData, context.ViewData);
            Assert.Null(context.ViewBag.Hello);
            Assert.Equal("property", context.ViewBag.Another);
            Assert.Equal("property", context.ViewData["Another"]);
        }
    }
}
