// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.ResponseCaching
{
    internal static class StreamUtilities
    {
        /// <summary>
        /// The segment size for buffering the response body in bytes. The default is set to 80 KB (81920 Bytes) to avoid allocations on the LOH.
        /// </summary>
        // Internal for testing
        internal static int BodySegmentSize { get; set; } = 81920;
    }
}
