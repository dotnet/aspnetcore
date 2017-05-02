// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore
{
    internal interface ICertificateFileLoader
    {
        X509Certificate2 Load(string path, string password, X509KeyStorageFlags flags);
    }
}
