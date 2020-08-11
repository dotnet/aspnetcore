// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions
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

        protected override async ValueTask<int> CopyFileDataIntoBuffer(long sourceOffset, byte[] destination, int destinationOffset, int maxBytes, CancellationToken cancellationToken)
        {
            await _jsRuntime.InvokeVoidAsync(InputFileInterop.EnsureArrayBufferReadyForSharedMemoryInterop, cancellationToken, _inputFileElement, File.Id);

            return _jsUnmarshalledRuntime.InvokeUnmarshalled<ReadRequest, int>(InputFileInterop.ReadFileDataSharedMemory, new ReadRequest
            {
                InputFileElementReferenceId = _inputFileElement.Id,
                FileId = File.Id,
                SourceOffset = sourceOffset,
                Destination = destination,
                DestinationOffset = destinationOffset,
                MaxBytes = maxBytes
            });
        }
    }
}
