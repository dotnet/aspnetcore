// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Connection;

internal class DirectSslConnectionContext : ConnectionContext
{
    public DirectSslConnectionContext()
    {
        
    }

    public override IDuplexPipe Transport { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public override string ConnectionId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override IFeatureCollection Features => throw new NotImplementedException();

    public override IDictionary<object, object?> Items { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}
