// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesWebSite.Models;

namespace RazorPagesWebSite
{
    public class RenderPartialWithModel : PageModel
    {
        public string Text { get; set; } = $"Hello from {nameof(RenderPartialWithModel)}";

        public IActionResult OnGet() => Partial("_RenderPartial", new RenderPartialModel { Value = $"Hello from {nameof(RenderPartialModel)}" });

        public IActionResult OnGetUsePageModelAsPartialModel() => Partial("_RenderPartialPageModel", this);

        public IActionResult OnGetNoPartialModel() => Partial("_RenderPartial");
    }
}
