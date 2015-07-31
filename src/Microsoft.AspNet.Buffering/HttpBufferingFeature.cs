// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http.Features;

namespace Microsoft.AspNet.Buffering
{
    internal class HttpBufferingFeature : IHttpBufferingFeature
    {
        private readonly BufferingWriteStream _buffer;
        private readonly IHttpBufferingFeature _innerFeature;

        internal HttpBufferingFeature(BufferingWriteStream buffer, IHttpBufferingFeature innerFeature)
        {
            _buffer = buffer;
            _innerFeature = innerFeature;
        }

        public void DisableRequestBuffering()
        {
            _innerFeature?.DisableRequestBuffering();
        }

        public void DisableResponseBuffering()
        {
            _buffer.DisableBuffering();
            _innerFeature?.DisableResponseBuffering();
        }
    }
}
