// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace TagHelpersWebSite
{
    public class CopyrightViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string website, int year)
        {
            var dict = new Dictionary<string, object>
            {
                ["website"] = website,
                ["year"] = year
            };

            return View(dict);
        }
    }
}
