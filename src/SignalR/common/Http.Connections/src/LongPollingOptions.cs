// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Http.Connections
{
    /// <summary>
    /// Options used to configure the long polling transport.
    /// </summary>
    public class LongPollingOptions
    {
        /// <summary>
        /// Gets or sets the poll timeout.
        /// </summary>
        public TimeSpan PollTimeout { get; set; } = TimeSpan.FromSeconds(90);
    }
}
