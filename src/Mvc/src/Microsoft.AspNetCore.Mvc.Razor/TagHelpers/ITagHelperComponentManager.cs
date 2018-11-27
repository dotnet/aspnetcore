// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers
{
    /// <summary>
    /// An implementation of this interface provides the collection of <see cref="ITagHelperComponent"/>s
    /// that will be used by <see cref="TagHelperComponentTagHelper"/>s.
    /// </summary>
    public interface ITagHelperComponentManager
    {
        /// <summary>
        /// Gets the collection of <see cref="ITagHelperComponent"/>s that will be used by
        /// <see cref="TagHelperComponentTagHelper"/>s.
        /// </summary>
        ICollection<ITagHelperComponent> Components { get; }
    }
}
