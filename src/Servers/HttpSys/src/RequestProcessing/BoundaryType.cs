// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.HttpSys;

internal enum BoundaryType
{
    None = 0,
    Chunked = 1, // Transfer-Encoding: chunked
    ContentLength = 2, // Content-Length: XXX
    Close = 3, // Connection: close
    PassThrough = 4, // The application is handling the boundary themselves (e.g. chunking themselves).
    Invalid = 5,
}
