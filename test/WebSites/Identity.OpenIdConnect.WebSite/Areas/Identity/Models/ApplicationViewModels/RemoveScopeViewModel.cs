// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Identity.OpenIdConnect.WebSite.Identity.Models.ApplicationViewModels
{
    public class RemoveScopeViewModel
    {
        public RemoveScopeViewModel(string name, string scope)
        {
            Name = name;
            Scope = scope;
        }

        public string Name { get; }
        public string Scope { get; }
    }
}
