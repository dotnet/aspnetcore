// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite;

public class ModelHandlerTestModel : PageModel
{
    public string MethodName { get; set; }

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await Task.CompletedTask;
        MethodName = nameof(OnPostAsync);
        return Page();
    }

    public async Task OnGetCustomer()
    {
        await Task.CompletedTask;
        MethodName = nameof(OnGetCustomer);
    }

    public async Task OnGetViewCustomerAsync()
    {
        await Task.CompletedTask;
        MethodName = nameof(OnGetViewCustomerAsync);
    }

    public IActionResult OnGetDefaultValues(
        bool boolean,
        int id = 10,
        Guid guid = default(Guid),
        DateTime dateTime = default(DateTime))
    {
        return Content($"id: {id}, guid: {guid}, boolean: {boolean}, dateTime: {dateTime}");
    }

    public async Task<CustomActionResult> OnPostCustomActionResult()
    {
        await Task.CompletedTask;
        return new CustomActionResult();
    }

    public CustomActionResult OnGetCustomActionResultAsync()
    {
        return new CustomActionResult();
    }
}
