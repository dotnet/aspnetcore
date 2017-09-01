// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Identity.OpenIdConnect.WebSite.Identity.Models.ApplicationViewModels
{
    public class CreateLogoutUriViewModel
    {
        public CreateLogoutUriViewModel()
        {
        }

        public CreateLogoutUriViewModel(string id, string applicationName, IEnumerable<string> logoutUris)
        {
            Id = id;
            Name = applicationName;
            LogoutUris = logoutUris;
        }

        public string Id { get; set; }
        public string Name { get; }
        public IEnumerable<string> LogoutUris { get; }
        public string NewLogoutUri { get; set; }
    }
}
