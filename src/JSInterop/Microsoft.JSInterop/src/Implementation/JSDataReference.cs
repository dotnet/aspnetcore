// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.JSInterop.Implementation
{
    /// <summary>
    /// Implements functionality for <see cref="IJSDataReference"/>.
    /// </summary>
    public sealed class JSDataReference : JSObjectReference, IJSDataReference
    {
        private readonly JSRuntime _jsRuntime;

        /// <inheritdoc />
        public long Length { get; }

        /// <summary>
        /// Inititializes a new <see cref="JSDataReference"/> instance.
        /// </summary>
        /// <param name="jsRuntime">The <see cref="JSRuntime"/> used for invoking JS interop calls.</param>
        /// <param name="id">The unique identifier.</param>
        /// <param name="totalLength">The length of the data stream coming from JS represented by this data reference.</param>
        internal JSDataReference(JSRuntime jsRuntime, long id, long totalLength) : base(jsRuntime, id)
        {
            if (totalLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalLength), totalLength, "Length must be a positive value.");
            }

            _jsRuntime = jsRuntime;
            Length = totalLength;
        }

        /// <inheritdoc />
        async ValueTask<Stream> IJSDataReference.OpenReadStreamAsync(long maxLength, long maxBufferSize, CancellationToken cancellationToken)
        {
            if (Length > maxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLength), $"The incoming data stream of length {Length} exceeds the maximum length {maxLength}.");
            }

            return await _jsRuntime.ReadJSDataAsStreamAsync(this, Length, maxBufferSize, cancellationToken);
        }
    }
}
