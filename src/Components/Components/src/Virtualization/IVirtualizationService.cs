// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Virtualization
{
    /// <summary>
    /// Describes services enabling platform-specific virtualization.
    /// </summary>
    public interface IVirtualizationService
    {
        /// <summary>
        /// Creates a helper used for detecting and relaying virtualization events.
        /// </summary>
        /// <returns></returns>
        IVirtualizationHelper CreateVirtualizationHelper();
    }
}
