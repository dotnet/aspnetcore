// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Threading;

namespace Microsoft.AspNetCore.Components.Web.Virtualization
{
    /// <summary>
    /// Represents a request to an <see cref="ItemsProviderDelegate{TItem}"/>.
    /// </summary>
    public readonly struct ItemsProviderRequest
    {
        /// <summary>
        /// The start index of the data segment requested.
        /// </summary>
        public int StartIndex { get; }

        /// <summary>
        /// The requested number of items to be provided. The actual number of provided items does not need to match
        /// this value.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// The <see cref="System.Threading.CancellationToken"/> used to relay cancellation of the request.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Constructs a new <see cref="ItemsProviderRequest"/> instance.
        /// </summary>
        /// <param name="startIndex">The start index of the data segment requested.</param>
        /// <param name="count">The requested number of items to be provided.</param>
        /// <param name="cancellationToken">
        /// The <see cref="System.Threading.CancellationToken"/> used to relay cancellation of the request.
        /// </param>
        public ItemsProviderRequest(int startIndex, int count, CancellationToken cancellationToken)
        {
            StartIndex = startIndex;
            Count = count;
            CancellationToken = cancellationToken;
        }
    }
}
