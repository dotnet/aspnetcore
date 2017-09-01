// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Identity.OpenIdConnect.WebSite.Identity.Models.ApplicationViewModels
{
    public class EditScopeViewModel
    {
        public EditScopeViewModel()
        {
        }

        public EditScopeViewModel(string applicationName, string scope)
        {
            Name = applicationName;
            Scope = scope;
        }

        public string Name { get; }
        public string Scope { get; set; }
    }
}
