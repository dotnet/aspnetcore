// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// A mapping of a <see cref="AspNetCore.Razor.TagHelpers.ITagHelper"/> mode to its required attributes.
    /// </summary>
    /// <typeparam name="TMode">The type representing the <see cref="AspNetCore.Razor.TagHelpers.ITagHelper"/>'s mode.</typeparam>
    internal class ModeAttributes<TMode>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ModeAttributes{TMode}"/>.
        /// </summary>
        /// <param name="mode">The <see cref="AspNetCore.Razor.TagHelpers.ITagHelper"/>'s mode.</param>
        /// <param name="attributes">The names of attributes required for this mode.</param>
        public ModeAttributes(TMode mode, string[] attributes)
        {
            Mode = mode;
            Attributes = attributes;
        }

        /// <summary>
        /// Gets the <see cref="AspNetCore.Razor.TagHelpers.ITagHelper"/>'s mode.
        /// </summary>
        public TMode Mode { get; }

        /// <summary>
        /// Gets the names of attributes required for this mode.
        /// </summary>
        public string[] Attributes { get; }
    }
}
