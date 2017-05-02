// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore
{
    internal interface ICertificateStoreLoader
    {
        X509Certificate2 Load(string subject, string storeName, StoreLocation storeLocation, bool validOnly);
    }
}
