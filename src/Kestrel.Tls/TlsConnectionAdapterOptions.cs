// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Tls
{
    public class TlsConnectionAdapterOptions
    {
        public string CertificatePath { get; set; } = string.Empty;

        public string PrivateKeyPath { get; set; } = string.Empty;
    }
}
