// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public class Http2MessageBody : MessageBody
    {
        private readonly Http2Stream _context;

        private Http2MessageBody(Http2Stream context, MinDataRate minRequestBodyDataRate)
            : base(context, minRequestBodyDataRate)
        {
            _context = context;
        }

        protected override void OnReadStarting()
        {
            // Note ContentLength or MaxRequestBodySize may be null
            if (_context.RequestHeaders.ContentLength > _context.MaxRequestBodySize)
            {
                BadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTooLarge);
            }
        }

        protected override void OnReadStarted()
        {
            // Produce 100-continue if no request body data for the stream has arrived yet.
            if (!_context.RequestBodyStarted)
            {
                TryProduceContinue();
            }
        }

        protected override void OnDataRead(long bytesRead)
        {
            // The HTTP/2 flow control window cannot be larger than 2^31-1 which limits bytesRead.
            _context.OnDataRead((int)bytesRead);
            AddAndCheckConsumedBytes(bytesRead);
        }

        public static MessageBody For(Http2Stream context, MinDataRate minRequestBodyDataRate)
        {
            if (context.ReceivedEmptyRequestBody)
            {
                return ZeroContentLengthClose;
            }

            return new Http2MessageBody(context, minRequestBodyDataRate);
        }
    }
}
