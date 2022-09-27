// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite;

[ResponseCache(Duration = 10, Location = ResponseCacheLocation.Client)]
public class ModelWithResponseCache : PageModel
{
    public string Message { get; set; }

    public void OnGet()
    {
        Message = $"Hello from {nameof(ModelWithResponseCache)}.{nameof(OnGet)}";
    }
}
