// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    /// <summary>
    /// A feature that contains view descriptors.
    /// </summary>
    public class ViewsFeature
    {
        /// <summary>
        /// A list of <see cref="CompiledViewDescriptor"/>.
        /// </summary>
        public IList<CompiledViewDescriptor> ViewDescriptors { get; } = new List<CompiledViewDescriptor>();
    }
}
