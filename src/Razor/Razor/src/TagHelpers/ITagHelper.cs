// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    /// <summary>
    /// Contract used to filter matching HTML elements.
    /// Marker interface for <see cref="TagHelper"/>s.
    /// </summary>
    public interface ITagHelper : ITagHelperComponent
    {
    }
}