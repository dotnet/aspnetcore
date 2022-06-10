// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
internal abstract class Http3StreamBase : IHttp3Stream, IThreadPoolWorkItem // TODO unite the common elements of uni and bi directional streams
{
    long IHttp3Stream.StreamId => throw new NotImplementedException(); //_streamIdFeature.StreamId;

    long IHttp3Stream.StreamTimeoutTicks { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    bool IHttp3Stream.IsReceivingHeader => throw new NotImplementedException();

    bool IHttp3Stream.IsDraining => throw new NotImplementedException();

    bool IHttp3Stream.IsRequestStream => throw new NotImplementedException();

    string IHttp3Stream.TraceIdentifier => throw new NotImplementedException();

    void IHttp3Stream.Abort(ConnectionAbortedException abortReason, Http3ErrorCode errorCode)
    {
        throw new NotImplementedException();
    }

    void IThreadPoolWorkItem.Execute()
    {
        throw new NotImplementedException();
    }
}
