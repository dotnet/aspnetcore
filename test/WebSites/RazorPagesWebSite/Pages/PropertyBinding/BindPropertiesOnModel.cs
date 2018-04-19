// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite
{
    [BindProperties]
    public class BindPropertiesOnModel : PageModel
    {
        [FromQuery]
        public string Property1 { get; set; }

        public string Property2 { get; set; }
    }
}
