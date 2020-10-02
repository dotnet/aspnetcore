// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Diagnostics
{
    /// <summary>Default implementation for <see cref="IStatusCodeReExecuteFeature" />.</summary>
    public class StatusCodeReExecuteFeature : IStatusCodeReExecuteFeature
    {
        /// <inheritdoc/>
        public string OriginalPath { get; set; }

        /// <inheritdoc/>
        public string OriginalPathBase { get; set; }

        /// <inheritdoc/>
        public string OriginalQueryString { get; set; }
    }
}
