// -----------------------------------------------------------------------
// <copyright file="SslStatus.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Net.Server
{
    internal enum SslStatus : byte
    {
        Insecure,
        NoClientCert,
        ClientCert
    }
}
