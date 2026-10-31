// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesWebSite.Models;

namespace RazorPagesWebSite;

public class RenderPartialWithModel : PageModel
{
    public string Text { get; set; } = $"Hello from {nameof(RenderPartialWithModel)}";

    public IActionResult OnGet() => Partial("_RenderPartial", new RenderPartialModel { Value = $"Hello from {nameof(RenderPartialModel)}" });

    public IActionResult OnGetUsePageModelAsPartialModel() => Partial("_RenderPartialPageModel", this);

    public IActionResult OnGetNoPartialModel() => Partial("_RenderPartial");
}
