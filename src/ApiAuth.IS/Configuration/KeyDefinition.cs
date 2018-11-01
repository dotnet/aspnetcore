// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Configuration
{
    internal class KeyDefinition
    {
        public string Type { get; set; }
        public bool? Persisted { get; set; }
        public string FilePath { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string StoreLocation { get; set; }
        public string StoreName { get; set; }
        public string StorageFlags { get; set; }
    }
}
