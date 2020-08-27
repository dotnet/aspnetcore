// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Forms
{
    internal class SharedBrowserFileStream : BrowserFileStream
    {
        private readonly IJSRuntime _jsRuntime;

        private readonly IJSUnmarshalledRuntime _jsUnmarshalledRuntime;

        private readonly ElementReference _inputFileElement;

        public SharedBrowserFileStream(IJSRuntime jsRuntime, IJSUnmarshalledRuntime jsUnmarshalledRuntime, ElementReference inputFileElement, BrowserFile file)
            : base(file)
        {
            _jsRuntime = jsRuntime;
            _jsUnmarshalledRuntime = jsUnmarshalledRuntime;
            _inputFileElement = inputFileElement;
        }

        protected override async ValueTask<int> CopyFileDataIntoBuffer(long sourceOffset, Memory<byte> destination, CancellationToken cancellationToken)
        {
            await _jsRuntime.InvokeVoidAsync(InputFileInterop.EnsureArrayBufferReadyForSharedMemoryInterop, cancellationToken, _inputFileElement, File.Id);

            var readRequest = new ReadRequest
            {
                InputFileElementReferenceId = _inputFileElement.Id,
                FileId = File.Id,
                SourceOffset = sourceOffset
            };

            if (MemoryMarshal.TryGetArray(destination, out ArraySegment<byte> destinationArraySegment))
            {
                readRequest.Destination = destinationArraySegment.Array!;
                readRequest.DestinationOffset = destinationArraySegment.Offset;
                readRequest.MaxBytes = destinationArraySegment.Count;
            }
            else
            {
                // Worst case, we need to copy to a temporary array.
                readRequest.Destination = new byte[destination.Length];
                readRequest.DestinationOffset = 0;
                readRequest.MaxBytes = destination.Length;

                destination.CopyTo(new Memory<byte>(readRequest.Destination));
            }

            return _jsUnmarshalledRuntime.InvokeUnmarshalled<ReadRequest, int>(InputFileInterop.ReadFileDataSharedMemory, readRequest);
        }
    }
}
