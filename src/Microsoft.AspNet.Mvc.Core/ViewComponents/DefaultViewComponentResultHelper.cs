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

        public virtual ContentViewComponentResult Content([NotNull] string content)
        {
            return new ContentViewComponentResult(content);
        }

        public virtual JsonViewComponentResult Json([NotNull] object value)
        {
            return new JsonViewComponentResult(value);
        }

        public virtual ViewViewComponentResult View([NotNull] string viewName, [NotNull] ViewDataDictionary viewData)
        {
            return new ViewViewComponentResult(_viewEngine, viewName, viewData);
        }
    }
}
