// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Components
{
    public class PassThroughViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(long value)
        {
            return View(value);
        }
    }
}
