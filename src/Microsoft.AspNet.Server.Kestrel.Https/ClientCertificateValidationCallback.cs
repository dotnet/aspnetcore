// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNet.Server.Kestrel.Https
{
    public delegate bool ClientCertificateValidationCallback(
        X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors);
}
