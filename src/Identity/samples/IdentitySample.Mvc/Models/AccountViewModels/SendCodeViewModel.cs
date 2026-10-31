// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;

namespace IdentitySample.Models.AccountViewModels;

public class SendCodeViewModel
{
    public string SelectedProvider { get; set; }

    public ICollection<SelectListItem> Providers { get; set; }

    public string ReturnUrl { get; set; }

    public bool RememberMe { get; set; }
}
