// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite.Components
{
    [ViewComponent(Name = "InheritingViewComponent")]
    public class InheritingViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Address address)
        {
            return View("/Views/InheritingInherits/_ViewComponent.cshtml", address);
        }
    }
}
