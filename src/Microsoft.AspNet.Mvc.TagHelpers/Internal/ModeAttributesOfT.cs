// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    /// <summary>
    /// A mapping of a <see cref="AspNet.Razor.TagHelpers.ITagHelper"/> mode to its required attributes.
    /// </summary>
    /// <typeparam name="TMode">The type representing the <see cref="AspNet.Razor.TagHelpers.ITagHelper"/>'s mode.</typeparam>
    public class ModeAttributes<TMode>
    {
        /// <summary>
        /// The <see cref="AspNet.Razor.TagHelpers.ITagHelper"/>'s mode.
        /// </summary>
        public TMode Mode { get; set; }

        /// <summary>
        /// The names of attributes required for this mode.
        /// </summary>
        public string[] Attributes { get; set; }
    }
}
