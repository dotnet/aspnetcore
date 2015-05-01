// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;

namespace CompositeViewEngineWebSite
{
    public class TestViewEngine : IViewEngine
    {
        public ViewEngineResult FindPartialView(ActionContext context, string partialViewName)
        {
            if (string.Equals(partialViewName, "partial-test-view", StringComparison.Ordinal))
            {
                return ViewEngineResult.Found(partialViewName, new TestPartialView());
            }
            return ViewEngineResult.NotFound(partialViewName, new[] { partialViewName });
        }

        public ViewEngineResult FindView(ActionContext context, string viewName)
        {
            if (string.Equals(viewName, "test-view"))
            {
                return ViewEngineResult.Found(viewName, new TestView());
            }
            return ViewEngineResult.NotFound(viewName, new[] { viewName });
        }
    }
}