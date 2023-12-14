// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

internal partial class Http2Stream : IHttp2StreamIdFeature,
                                     IHttpMinRequestBodyDataRateFeature,
                                     IHttpResetFeature,
                                     IHttpResponseTrailersFeature,
                                     IPersistentStateFeature
{
    private IHeaderDictionary? _userTrailers;

    // Persistent state collection is not reset with a stream by design.
    private IDictionary<object, object?>? _persistentState;

    IHeaderDictionary IHttpResponseTrailersFeature.Trailers
    {
        get
        {
            if (ResponseTrailers == null)
            {
                ResponseTrailers = new HttpResponseTrailers(ServerOptions.ResponseHeaderEncodingSelector);
                if (HasResponseCompleted)
                {
                    ResponseTrailers.SetReadOnly();
                }
            }
            return _userTrailers ?? ResponseTrailers;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _userTrailers = value;
        }
    }

    int IHttp2StreamIdFeature.StreamId => _context.StreamId;

    MinDataRate? IHttpMinRequestBodyDataRateFeature.MinDataRate
    {
        get => throw new NotSupportedException(CoreStrings.HttpMinDataRateNotSupported);
        set
        {
            if (value != null)
            {
                throw new NotSupportedException(CoreStrings.HttpMinDataRateNotSupported);
            }

            MinRequestBodyDataRate = value;
        }
    }

    void IHttpResetFeature.Reset(int errorCode)
    {
        var abortReason = new ConnectionAbortedException(CoreStrings.FormatHttp2StreamResetByApplication((Http2ErrorCode)errorCode));
        ApplicationAbort(abortReason, (Http2ErrorCode)errorCode);
    }

    IDictionary<object, object?> IPersistentStateFeature.State
    {
        get
        {
            // Lazily allocate persistent state
            return _persistentState ?? (_persistentState = new ConnectionItems());
        }
    }
}
