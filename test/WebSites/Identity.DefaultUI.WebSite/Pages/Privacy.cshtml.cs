// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Identity.DefaultUI.WebSite.Pages
{
    public class PrivacyModel : PageModel
    {
        public void OnGet()
        {
            HttpContext.Features.Get<ITrackingConsentFeature>().GrantConsent();
        }
    }
}