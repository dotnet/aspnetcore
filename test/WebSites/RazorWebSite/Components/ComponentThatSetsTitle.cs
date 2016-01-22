// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace MvcSample.Web.Components
{
    [ViewComponent(Name = "ComponentThatSetsTitle")]
    public class ComponentThatSetsTitle : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}