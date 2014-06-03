// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public interface IViewComponentResultHelper
    {
        ContentViewComponentResult Content([NotNull] string content);

        JsonViewComponentResult Json([NotNull] object value);

        ViewViewComponentResult View([NotNull] string viewName, [NotNull] ViewDataDictionary viewData);
    }
}
