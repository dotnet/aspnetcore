// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultViewComponentResultHelper : IViewComponentResultHelper
    {
        private readonly IViewEngine _viewEngine;

        public DefaultViewComponentResultHelper(IViewEngine viewEngine)
        {
            _viewEngine = viewEngine;
        }

        public IViewComponentResult Content([NotNull] string content)
        {
            return new ContentViewComponentResult(content);
        }

        public IViewComponentResult Json([NotNull] object value)
        {
            return new JsonViewComponentResult(value);
        }

        public IViewComponentResult View([NotNull] string viewName, [NotNull] ViewDataDictionary viewData)
        {
            return new ViewViewComponentResult(_viewEngine, viewName, viewData);
        }
    }
}
