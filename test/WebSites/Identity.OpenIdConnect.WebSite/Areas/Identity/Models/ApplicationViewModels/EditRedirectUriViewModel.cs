// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Identity.OpenIdConnect.WebSite.Identity.Models.ApplicationViewModels
{
    public class EditRedirectUriViewModel
    {
        public EditRedirectUriViewModel()
        {
        }

        public EditRedirectUriViewModel(string applicationName, string redirectUri)
        {
            Name = applicationName;
            RedirectUri = redirectUri;
        }

        public string Name { get; }
        public string RedirectUri { get; set; }
    }
}
