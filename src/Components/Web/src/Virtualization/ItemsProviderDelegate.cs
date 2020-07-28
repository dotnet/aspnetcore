// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    /// <summary>
    /// A function that provides items to a virtualized source.
    /// </summary>
    /// <typeparam name="TItem">The type of the context for each item in the list.</typeparam>
    /// <param name="startIndex">The start index of the data segment requested.</param>
    /// <param name="requestedCount">
    /// The requested number of items to be provided. The actual number of provided items does not need to match
    /// this value.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to relay cancellation of the operation.</param>
    /// <returns>A <see cref="Task"/> whose result is a <see cref="ItemsProviderResult{TItem}"/> upon successful completion.</returns>
    public delegate Task<ItemsProviderResult<TItem>> ItemsProviderDelegate<TItem>(
        int startIndex,
        int requestedCount,
        CancellationToken cancellationToken);
}
