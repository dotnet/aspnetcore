// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace TagHelpersWebSite.Components
{
    public class GenericViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Dictionary<string, List<string>> items)
        {
            return View(items);
        }
    }
}
