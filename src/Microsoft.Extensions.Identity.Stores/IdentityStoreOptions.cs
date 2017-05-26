// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Used for store schema versioning.
    /// </summary>
    public class IdentityStoreOptions
    {
        /// <summary>
        /// Matches version 1.x.x
        /// </summary>
        public const string Version1_0 = "v1.0";

        /// <summary>
        /// Matches version 2.0.0
        /// </summary>
        public const string Version2_0 = "v2.0";

        /// <summary>
        /// Used to represent the most current version.
        /// </summary>
        public const string Version_Latest = "latest";

        /// <summary>
        /// Used to determine what features/schema are supported in the store.
        /// </summary>
        public string Version { get; set; }
    }
}