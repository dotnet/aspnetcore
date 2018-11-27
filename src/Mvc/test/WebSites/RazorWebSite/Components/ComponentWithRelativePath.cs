// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite.Components
{
    public class ComponentWithRelativePath : ViewComponent
    {
        public IViewComponentResult Invoke(Person person)
        {
            return View("../Shared/Components/ComponentWithRelativePath.cshtml", person);
        }
    }
}