// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Authentication.WebAssembly.Msal.Models
{
    public class MsalCacheOptions
    {
        public string CacheLocation { get; set; }

        public bool StoreAuthStateInCookie { get; set; }
    }
}
