// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class ViewContextTests
    {
        [Fact]
        public void SettingViewData_AlsoUpdatesViewBag()
        {
            // Arrange (eventually passing null to these consturctors will throw)
            var context = new ViewContext(new ActionContext(null, null, null), view: null, viewData: null, tempData: null, writer: null);
            var originalViewData = context.ViewData = new ViewDataDictionary(metadataProvider: new EmptyModelMetadataProvider());
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
