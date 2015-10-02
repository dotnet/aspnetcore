// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNet.Http.Features;

namespace Microsoft.AspNet.Hosting.Internal
{
    public class FastHttpRequestIdentifierFeature : IHttpRequestIdentifierFeature
    {
        private static readonly string _hexChars = "0123456789ABCDEF";
        // Seed the _requestId for this application instance with a random int
        private static long _requestId = new Random().Next();
        
        private string _id = null;
        
        public string TraceIdentifier
        {
            get
            {
                // Don't incur the cost of generating the request ID until it's asked for
                if (_id == null)
                {
                    _id = GenerateRequestId(Interlocked.Increment(ref _requestId));
                }
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        private static string GenerateRequestId(long id)
        {
            // The following routine is ~33% faster than calling long.ToString() when testing in tight loops of
            // 1 million iterations.
            var charBuffer = new char[sizeof(long) * 2];
            charBuffer[0] = _hexChars[(int)(id >> 60) & 0x0f];
            charBuffer[1] = _hexChars[(int)(id >> 56) & 0x0f];
            charBuffer[2] = _hexChars[(int)(id >> 52) & 0x0f];
            charBuffer[3] = _hexChars[(int)(id >> 48) & 0x0f];
            charBuffer[4] = _hexChars[(int)(id >> 44) & 0x0f];
            charBuffer[5] = _hexChars[(int)(id >> 40) & 0x0f];
            charBuffer[6] = _hexChars[(int)(id >> 36) & 0x0f];
            charBuffer[7] = _hexChars[(int)(id >> 32) & 0x0f];
            charBuffer[8] = _hexChars[(int)(id >> 28) & 0x0f];
            charBuffer[9] = _hexChars[(int)(id >> 24) & 0x0f];
            charBuffer[10] = _hexChars[(int)(id >> 20) & 0x0f];
            charBuffer[11] = _hexChars[(int)(id >> 16) & 0x0f];
            charBuffer[12] = _hexChars[(int)(id >> 12) & 0x0f];
            charBuffer[13] = _hexChars[(int)(id >> 8) & 0x0f];
            charBuffer[14] = _hexChars[(int)(id >> 4) & 0x0f];
            charBuffer[15] = _hexChars[(int)(id >> 0) & 0x0f];

            return new string(charBuffer);
        }
    }
}
