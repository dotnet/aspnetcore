// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public partial class Http2Stream : IHttp2StreamIdFeature, IHttpResponseTrailersFeature
    {
        internal HttpResponseTrailers Trailers { get; set; }
        private IHeaderDictionary _userTrailers;

        IHeaderDictionary IHttpResponseTrailersFeature.Trailers
        {
            get
            {
                if (Trailers == null)
                {
                    Trailers = new HttpResponseTrailers();
                }
                return _userTrailers ?? Trailers;
            }
            set
            {
                _userTrailers = value;
            }
        }

        int IHttp2StreamIdFeature.StreamId => _context.StreamId;
    }
}
