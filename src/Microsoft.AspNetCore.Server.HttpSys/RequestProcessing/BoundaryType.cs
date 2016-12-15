// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal enum BoundaryType
    {
        None = 0,
        Chunked = 1, // Transfer-Encoding: chunked
        ContentLength = 2, // Content-Length: XXX
        Close = 3, // Connection: close
        PassThrough = 4, // The application is handling the boundary themselves (e.g. chunking themselves).
        Invalid = 5,
    }
}
