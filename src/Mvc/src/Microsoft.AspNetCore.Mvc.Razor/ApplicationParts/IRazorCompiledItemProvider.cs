// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Hosting;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// Exposes one or more <see cref="RazorCompiledItem"/> instances from an <see cref="ApplicationPart"/>.
    /// </summary>
    public interface IRazorCompiledItemProvider
    {
        /// <summary>
        /// Gets a sequence of <see cref="RazorCompiledItem"/> instances.
        /// </summary>
        IEnumerable<RazorCompiledItem> CompiledItems { get; }
    }
}
