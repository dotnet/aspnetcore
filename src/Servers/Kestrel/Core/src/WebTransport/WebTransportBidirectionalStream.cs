// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.WebTransport;

/// <summary>
/// Represents a WebTransport bidirectional stream
/// </summary>
[RequiresPreviewFeatures("WebTransport is a preview feature")]
public class WebTransportBidirectionalStream : WebTransportBaseStream
{
    private readonly WebTransportInputStream Input;
    private readonly WebTransportOutputStream Output;

    internal WebTransportBidirectionalStream(Http3StreamContext context) : base(context)
    {
        Input = new WebTransportInputStream(context);
        Output = new WebTransportOutputStream(context);
    }

    /// <summary>
    /// The message reading loop
    /// </summary>
    public override void Execute()
    {
        // both of these are async and start a new
        // while loop on a background thread
        Input.Execute();
        Output.Execute();
    }

    // TODO add methods here for managing the streams
    // also, add a reference from this into the streams so I can close and maintain them
}
