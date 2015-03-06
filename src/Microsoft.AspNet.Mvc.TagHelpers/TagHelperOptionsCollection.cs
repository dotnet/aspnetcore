// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    /// <summary>
    /// Used for adding options pertaining to <see cref="ITagHelper"/>s to an <see cref="IServiceCollection"/>.
    /// </summary>
    public class TagHelperOptionsCollection : ITagHelperOptionsCollection
    {
        /// <summary>
        /// Creates a new <see cref="TagHelperOptionsCollection"/>;
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/> instance to add the options to.</param>
        public TagHelperOptionsCollection([NotNull] IServiceCollection serviceCollection)
        {
            Services = serviceCollection;
        }

        /// <summary>
        /// The <see cref="IServiceCollection"/>.
        /// </summary>
        public IServiceCollection Services { get; }
    }
}