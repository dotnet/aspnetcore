// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Remove once HttpSys has enabled nullable
#nullable enable

namespace Microsoft.AspNetCore.HttpSys.Internal
{
    internal enum SslStatus : byte
    {
        Insecure,
        NoClientCert,
        ClientCert
    }
}
