// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Repesents configurable options for <see cref="BrowserFileStream"/> with Blazor Server.
/// </summary>
[UnsupportedOSPlatform("browser")]
[Obsolete("RemoteJSDataStream defaults are utilized instead of the options here.")]
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
    /// <para>
    /// This only has an effect when using Blazor Server.
    /// </para>
    /// </summary>
    public int MaxBufferSize { get; set; } = 1024 * 1024;

    /// <summary>
    /// Gets or sets the time limit for fetching a segment of file data.
    /// <para>
    /// This only has an effect when using Blazor Server.
    /// </para>
    /// </summary>
    public TimeSpan SegmentFetchTimeout { get; set; } = TimeSpan.FromMinutes(1);
}
