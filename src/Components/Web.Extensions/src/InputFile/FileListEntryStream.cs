// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal abstract class FileListEntryStream : Stream
    {
        private long _position;

        protected IJSRuntime JSRuntime { get; }

        protected ElementReference InputFileElement { get; }

        protected FileListEntry File { get; }

        protected FileListEntryStream(IJSRuntime jsRuntime, ElementReference inputFileElement, FileListEntry file)
        {
            JSRuntime = jsRuntime;
            InputFileElement = inputFileElement;
            File = file;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => File.Size;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
            => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException("Synchronous reads are not supported.");

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var maxBytesToRead = (int)Math.Min(count, Length - Position);

            if (maxBytesToRead == 0)
            {
                return 0;
            }

            var bytesRead = await CopyFileDataIntoBuffer(_position, buffer, offset, maxBytesToRead, cancellationToken);

            _position += bytesRead;

            File.InvokeOnDataRead();

            return bytesRead;
        }

        protected abstract Task<int> CopyFileDataIntoBuffer(long sourceOffset, byte[] destination, int destinationOffset, int maxBytes, CancellationToken cancellationToken);
    }
}
