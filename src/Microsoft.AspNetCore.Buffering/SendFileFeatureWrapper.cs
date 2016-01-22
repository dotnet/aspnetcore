// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Buffering
{
    internal class SendFileFeatureWrapper : IHttpSendFileFeature
    {
        private readonly IHttpSendFileFeature _originalSendFileFeature;
        private readonly BufferingWriteStream _bufferStream;

        public SendFileFeatureWrapper(IHttpSendFileFeature originalSendFileFeature, BufferingWriteStream bufferStream)
        {
            _originalSendFileFeature = originalSendFileFeature;
            _bufferStream = bufferStream;
        }

        // Flush and disable the buffer if anyone tries to call the SendFile feature.
        public async Task SendFileAsync(string path, long offset, long? length, CancellationToken cancellation)
        {
            await _bufferStream.DisableBufferingAsync(cancellation);
            await _originalSendFileFeature.SendFileAsync(path, offset, length, cancellation);
        }
    }
}