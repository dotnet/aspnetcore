// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicWebSite.Models;
using Microsoft.AspNet.Mvc;

namespace BasicWebSite.Components
{
    [ViewComponent(Name = "JsonTextInView")]
    public class JsonTextInView : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return Json(new Person()
            {
                Id = 10,
                Name = "John"
            });
        }
    }
}