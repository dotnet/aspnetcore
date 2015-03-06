// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Used for adding options pertaining to <see cref="ITagHelper"/>s to an <see cref="IServiceCollection"/>.
    /// </summary>
    public interface ITagHelperOptionsCollection
    {
        /// <summary>
        /// The <see cref="IServiceCollection"/>.
        /// </summary>
        IServiceCollection Services { get; }
    }
}