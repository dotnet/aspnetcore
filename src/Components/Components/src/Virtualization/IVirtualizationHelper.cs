// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    /// <summary>
    /// Describes a helper for detecting and relaying virtualization events.
    /// </summary>
    public interface IVirtualizationHelper : IAsyncDisposable
    {
        /// <summary>
        /// Performs platform-specific initialization.
        /// </summary>
        /// <param name="topSpacer">The <see cref="ElementReference"/> representing the top spacer.</param>
        /// <param name="bottomSpacer">The <see cref="ElementReference"/> representing the bottom spacer.</param>
        /// <returns>The <see cref="ValueTask"/> associated with the completion of this operation.</returns>
        ValueTask InitAsync(ElementReference topSpacer, ElementReference bottomSpacer);

        /// <summary>
        /// Invoked when the top spacer becomes visible.
        /// </summary>
        event EventHandler<SpacerEventArgs> TopSpacerVisible;

        /// <summary>
        /// Invoked when the bottom spacer becomes visible.
        /// </summary>
        event EventHandler<SpacerEventArgs> BottomSpacerVisible;
    }
}
