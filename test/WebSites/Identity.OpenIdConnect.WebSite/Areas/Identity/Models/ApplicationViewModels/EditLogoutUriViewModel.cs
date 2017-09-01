// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Identity.OpenIdConnect.WebSite.Identity.Models.ApplicationViewModels
{
    public class EditLogoutUriViewModel
    {
        public EditLogoutUriViewModel()
        {
        }

        public EditLogoutUriViewModel(string applicationName, string logoutUri)
        {
            Name = applicationName;
            LogoutUri = logoutUri;
        }

        public string Name { get; }
        public string LogoutUri { get; set; }
    }
}
