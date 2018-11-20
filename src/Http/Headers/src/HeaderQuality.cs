// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Net.Http.Headers
{
    public static class HeaderQuality
    {
        /// <summary>
        /// Quality factor to indicate a perfect match.
        /// </summary>
        public const double Match = 1.0;

        /// <summary>
        /// Quality factor to indicate no match.
        /// </summary>
        public const double NoMatch = 0.0;
    }
}