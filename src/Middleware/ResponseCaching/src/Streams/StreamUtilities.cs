// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.ResponseCaching;

internal static class StreamUtilities
{
    /// <summary>
    /// The segment size for buffering the response body in bytes. The default is set to 80 KB (81920 Bytes) to avoid allocations on the LOH.
    /// </summary>
    // Internal for testing
    internal static int BodySegmentSize { get; set; } = 81920;
}
