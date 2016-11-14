// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    /// <summary>
    /// The structure the element should be written in.
    /// </summary>
    internal enum TagStructure
    {
        /// <summary>
        /// If no other tag helper applies to the same element and specifies a <see cref="TagStructure"/>,
        /// <see cref="NormalOrSelfClosing"/> will be used.
        /// </summary>
        Unspecified,

        /// <summary>
        /// Element can be written as &lt;my-tag-helper&gt;&lt;/my-tag-helper&gt; or &lt;my-tag-helper /&gt;.
        /// </summary>
        NormalOrSelfClosing,

        /// <summary>
        /// Element can be written as &lt;my-tag-helper&gt; or &lt;my-tag-helper /&gt;.
        /// </summary>
        /// <remarks>Elements with a <see cref="WithoutEndTag"/> structure will never have any content.</remarks>
        WithoutEndTag
    }
}
