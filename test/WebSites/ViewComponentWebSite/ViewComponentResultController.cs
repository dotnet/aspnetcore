// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ViewComponentWebSite
{
    public class ViewComponentResultController : Controller
    {
        public IActionResult Invoke(int number)
        {
            return ViewComponent("Integer", number);
        }
    }
}
