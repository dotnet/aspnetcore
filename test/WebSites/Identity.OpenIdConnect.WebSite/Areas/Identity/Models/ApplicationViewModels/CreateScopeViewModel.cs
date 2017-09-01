// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Identity.OpenIdConnect.WebSite.Identity.Models.ApplicationViewModels
{
    public class CreateScopeViewModel
    {
        public CreateScopeViewModel()
        {
        }

        public CreateScopeViewModel(string applicationName, IEnumerable<string> scopes)
        {
            Name = applicationName;
            Scopes = scopes;
        }

        public string Name { get; }
        public IEnumerable<string> Scopes { get; }
        public string NewScope { get; set; }
    }
}
