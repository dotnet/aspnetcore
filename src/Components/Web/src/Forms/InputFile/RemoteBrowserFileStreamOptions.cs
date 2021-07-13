// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.Versioning;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Repesents configurable options for <see cref="BrowserFileStream"/> with Blazor Server.
    /// </summary>
    [UnsupportedOSPlatform("browser")]
    public class RemoteBrowserFileStreamOptions
    {
        /// <summary>
        /// Gets or sets the maximum segment size for file data sent over a SignalR circuit.
        /// The default value is 20K.
        /// <para>
        /// This only has an effect when using Blazor Server.
        /// </para>
        /// </summary>
        public int MaxSegmentSize { get; set; } = 20 * 1024; // SignalR limit is 32K.

        /// <summary>
        /// Gets or sets the maximum internal buffer size for unread data sent over a SignalR circuit.
        /// Default -1 represents the default Pipe pauseWriterThreshold in <see cref="System.IO.Pipelines.PipeOptions" />.
        /// <para>
        /// This only has an effect when using Blazor Server.
        /// </para>
        /// </summary>
        public int MaxBufferSize { get; set; } = -1;

        /// <summary>
        /// Gets or sets the time limit for fetching a segment of file data.
        /// <para>
        /// This only has an effect when using Blazor Server.
        /// </para>
        /// </summary>
        public TimeSpan SegmentFetchTimeout { get; set; } = TimeSpan.FromMinutes(1);
    }
}
