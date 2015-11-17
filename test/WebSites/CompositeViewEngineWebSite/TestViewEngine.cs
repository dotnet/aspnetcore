// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ViewEngines;

namespace CompositeViewEngineWebSite
{
    public class TestViewEngine : IViewEngine
    {
        public ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage)
        {
            if (string.Equals(viewName, "partial-test-view", StringComparison.Ordinal) ||
                string.Equals(viewName, "test-view", StringComparison.Ordinal))
            {
                var view = isMainPage ? (IView)new TestView() : new TestPartialView();

                return ViewEngineResult.Found(viewName, view);
            }

            return ViewEngineResult.NotFound(viewName, Enumerable.Empty<string>());
        }

        public ViewEngineResult GetView(string executingFilePath, string viewPath, bool isMainPage)
        {
            return ViewEngineResult.NotFound(viewPath, Enumerable.Empty<string>());
        }
    }
}