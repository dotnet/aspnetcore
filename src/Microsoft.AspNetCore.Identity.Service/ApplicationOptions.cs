// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class ApplicationOptions
    {
        public string AllowedNameCharacters { get; set; } = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        public int? MaxApplicationNameLength { get; set; } = 36;
        public string AllowedClientIdCharacters { get; set; } = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        public int? MaxClientIdLength { get; set; } = 36;
        public string AllowedScopeCharacters { get; set; } = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        public int? MaxScopeLength { get; set; } = 16;

        public IList<string> AllowedRedirectUris { get; set; } = new List<string>
        {
            "urn:ietf:wg:oauth:2.0:oob"
        };

        public IList<string> AllowedLogoutUris { get; set; } = new List<string>
        {
            "urn:ietf:wg:oauth:2.0:oob"
        };
    }
}
