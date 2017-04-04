// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public abstract class CompiledPageManifest
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CompiledPageManifest"/>.
        /// </summary>
        /// <param name="pages">The sequence of <see cref="CompiledPageInfo"/>.</param>
        public CompiledPageManifest(IReadOnlyList<CompiledPageInfo> pages)
        {
            CompiledPages = pages;
        }

        /// <summary>
        /// The <see cref="IReadOnlyList{T}"/> of <see cref="CompiledPageInfo"/>.
        /// </summary>
        public IReadOnlyList<CompiledPageInfo> CompiledPages { get; }
    }
}
