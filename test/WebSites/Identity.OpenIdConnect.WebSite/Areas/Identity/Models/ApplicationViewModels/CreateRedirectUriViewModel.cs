// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Identity.OpenIdConnect.WebSite.Identity.Models.ApplicationViewModels
{
    public class CreateRedirectUriViewModel
    {
        public CreateRedirectUriViewModel()
        {
        }

        public CreateRedirectUriViewModel(string applicationName, IEnumerable<string> redirectUris)
        {
            Name = applicationName;
            RedirectUris = redirectUris;
        }

        public string Name { get; }
        public IEnumerable<string> RedirectUris { get; }
        public string NewRedirectUri { get; set; }
    }
}
