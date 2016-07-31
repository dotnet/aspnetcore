// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    internal class SendFileFeatureWrapper : IHttpSendFileFeature
    {
        private readonly IHttpSendFileFeature _originalSendFileFeature;
        private readonly ResponseCacheStream _responseCacheStream;

        public SendFileFeatureWrapper(IHttpSendFileFeature originalSendFileFeature, ResponseCacheStream responseCacheStream)
        {
            _originalSendFileFeature = originalSendFileFeature;
            _responseCacheStream = responseCacheStream;
        }

        // Flush and disable the buffer if anyone tries to call the SendFile feature.
        public Task SendFileAsync(string path, long offset, long? length, CancellationToken cancellation)
        {
            _responseCacheStream.DisableBuffering();
            return _originalSendFileFeature.SendFileAsync(path, offset, length, cancellation);
        }
    }
}