// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite
{
    [BindProperties(SupportsGet = true)]
    public class BindPropertiesWithSupportsGetOnModel : PageModel
    {
        public string Property { get; set; }
    }
}
