// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Identity.OpenIdConnect.WebSite.Identity.Models.ApplicationViewModels
{
    public class GeneratedClientSecretViewModel
    {
        public GeneratedClientSecretViewModel(string name, string clientSecret)
        {
            Name = name;
            ClientSecret = clientSecret;
        }

        public string Name { get; }
        public string ClientSecret { get; }
    }
}
