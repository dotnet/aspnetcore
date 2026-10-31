// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite.Pages;

[IgnoreAntiforgeryToken]
public class UnionsModel : PageModel
{
    public IActionResult OnGet() => new JsonResult(new UnionBoolString(true));

    public IActionResult OnPost([FromBody] UnionBoolString value) => new JsonResult(value);
}

public union UnionBoolString(bool, string);
