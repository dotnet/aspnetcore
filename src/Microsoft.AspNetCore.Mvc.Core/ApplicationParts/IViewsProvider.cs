// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// Exposes a sequence of views associated with an <see cref="ApplicationPart"/> .
    /// </summary>
    public interface IViewsProvider
    {
        /// <summary>
        /// Gets the sequence of <see cref="ViewInfo"/>.
        /// </summary>
        IEnumerable<ViewInfo> Views { get; }
    }
}
