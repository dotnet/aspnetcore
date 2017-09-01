// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Identity.OpenIdConnect.WebSite.Identity.Models.ApplicationViewModels
{
    public class RemoveRedirectUriViewModel
    {
        public RemoveRedirectUriViewModel(string name, string redirectUri)
        {
            Name = name;
            RedirectUri = redirectUri;
        }

        public string Name { get; }
        public string RedirectUri { get; }
    }
}
