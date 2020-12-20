// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal partial class Http3Stream : IHttpResetFeature,
                                         IHttpResponseTrailersFeature
    {
        private IHeaderDictionary? _userTrailers;

        IHeaderDictionary IHttpResponseTrailersFeature.Trailers
        {
            get
            {
                if (ResponseTrailers == null)
                {
                    ResponseTrailers = new HttpResponseTrailers();
                    if (HasResponseCompleted)
                    {
                        ResponseTrailers.SetReadOnly();
                    }
                }
                return _userTrailers ?? ResponseTrailers;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _userTrailers = value;
            }
        }

        void IHttpResetFeature.Reset(int errorCode)
        {
            var abortReason = new ConnectionAbortedException(CoreStrings.FormatHttp3StreamResetByApplication((Http3ErrorCode)errorCode));
            ApplicationAbort(abortReason, (Http3ErrorCode)errorCode);
        }
    }
}
