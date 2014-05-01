// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public interface IViewEngine
    {
        ViewEngineResult FindView([NotNull] IDictionary<string, object> context, [NotNull] string viewName);

        ViewEngineResult FindPartialView(
            [NotNull] IDictionary<string, object> context,
            [NotNull] string partialViewName);
    }
}
