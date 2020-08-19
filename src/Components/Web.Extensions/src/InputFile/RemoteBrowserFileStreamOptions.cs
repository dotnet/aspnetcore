// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// Repesents configurable options for <see cref="RemoteBrowserFileStream"/>.
    /// </summary>
    public class RemoteBrowserFileStreamOptions
    {
        /// <summary>
        /// Gets or sets the maximum segment size for file data sent over a SignalR circuit.
        /// This only has an effect when using Blazor Server.
        /// </summary>
        public int SegmentSize { get; set; } = 20 * 1024; // SignalR limit is 32K.

        /// <summary>
        /// Gets or sets the maximum internal buffer size for unread data sent over a SignalR circuit.
        /// This only has an effect when using Blazor Server.
        /// </summary>
        public int MaxBufferSize { get; set; } = 1024 * 1024;

        /// <summary>
        /// Gets or sets the time limit for fetching a segment of file data.
        /// This only has an effect when using Blazor Server.
        /// </summary>
        public TimeSpan SegmentFetchTimeout { get; set; } = TimeSpan.FromSeconds(3);
    }
}
