// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;

namespace Microsoft.AspNetCore.Identity.Service.AzureKeyVault
{
    public class KeyVaultSigningCredentialsSourceOptions
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string VaultUri { get; set; }
        public string CertificateName { get; set; }
        public HttpClient ClientHandler { get; set; } = new HttpClient();
    }
}
