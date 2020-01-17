// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Testing
{
    /// <summary>
    /// https://en.wikipedia.org/wiki/Windows_10_version_history
    /// </summary>
    public static class WindowsVersions
    {
        public const string Win7 = "6.1";

        [Obsolete("Use Win7 instead.", error: true)]
        public const string Win2008R2 = Win7;

        public const string Win8 = "6.2";

        public const string Win81 = "6.3";

        public const string Win10 = "10.0";

        /// <summary>
        /// 1803, RS4, 17134
        /// </summary>
        public const string Win10_RS4 = "10.0.17134";

        /// <summary>
        /// 1809, RS5, 17763
        /// </summary>
        public const string Win10_RS5 = "10.0.17763";

        /// <summary>
        /// 1903, 19H1, 18362
        /// </summary>
        public const string Win10_19H1 = "10.0.18362";

        /// <summary>
        /// 1909, 19H2, 18363
        /// </summary>
        public const string Win10_19H2 = "10.0.18363";

        /// <summary>
        /// 2004, 20H1, 19033
        /// </summary>
        public const string Win10_20H1 = "10.0.19033";
    }
}
