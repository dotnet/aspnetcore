// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    /// <summary>
    /// A mapping of a <see cref="ITagHelper"/> mode to its required attributes.
    /// </summary>
    /// <typeparam name="TMode">The type representing the <see cref="ITagHelper"/>'s mode.</typeparam>
    public class ModeAttributes<TMode>
    {
        /// <summary>
        /// The <see cref="ITagHelper"/>'s mode.
        /// </summary>
        public TMode Mode { get; set; }

        /// <summary>
        /// The names of attributes required for this mode.
        /// </summary>
        public IEnumerable<string> Attributes { get; set; }
    }
}