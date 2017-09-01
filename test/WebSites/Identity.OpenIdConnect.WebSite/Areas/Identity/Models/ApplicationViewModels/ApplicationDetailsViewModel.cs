// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Identity.OpenIdConnect.WebSite.Identity.Models.ApplicationViewModels
{
    public class ApplicationDetailsViewModel
    {
        public string Name { get; set; }
        public string ClientId { get; set; }
        public bool HasClientSecret { get; set; }
        public IEnumerable<string> RedirectUris { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<string> LogoutUris { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<string> Scopes { get; set; } = Enumerable.Empty<string>();
    }
}
