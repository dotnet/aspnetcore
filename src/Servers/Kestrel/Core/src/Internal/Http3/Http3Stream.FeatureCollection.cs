// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal partial class Http3Stream : IHttpResetFeature,
                                     IHttpMinRequestBodyDataRateFeature,
                                     IHttpResponseTrailersFeature
{
    private IHeaderDictionary? _userTrailers;

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
        var message = CoreStrings.FormatHttp3StreamResetByApplication(Http3Formatting.ToFormattedErrorCode((Http3ErrorCode)errorCode));
        var abortReason = new ConnectionAbortedException(message);
        ApplicationAbort(abortReason, (Http3ErrorCode)errorCode);
    }
}
