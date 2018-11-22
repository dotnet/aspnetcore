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
        private readonly ResponseCachingStream _responseCachingStream;

        public SendFileFeatureWrapper(IHttpSendFileFeature originalSendFileFeature, ResponseCachingStream responseCachingStream)
        {
            _originalSendFileFeature = originalSendFileFeature;
            _responseCachingStream = responseCachingStream;
        }

        // Flush and disable the buffer if anyone tries to call the SendFile feature.
        public Task SendFileAsync(string path, long offset, long? length, CancellationToken cancellation)
        {
            _responseCachingStream.DisableBuffering();
            return _originalSendFileFeature.SendFileAsync(path, offset, length, cancellation);
        }
    }
}