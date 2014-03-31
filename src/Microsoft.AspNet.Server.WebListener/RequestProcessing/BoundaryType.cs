// -----------------------------------------------------------------------
// <copyright file="BoundaryType.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.AspNet.Server.WebListener
{
    internal enum BoundaryType
    {
        None = 0,
        Chunked = 1, // Transfer-Encoding: chunked
        ContentLength = 2, // Content-Length: XXX
        Invalid = 3,
    }
}
