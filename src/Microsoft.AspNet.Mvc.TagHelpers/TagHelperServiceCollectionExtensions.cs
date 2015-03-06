// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.TagHelpers;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class TagHelperServiceCollectionExtensions
    {
        /// <summary>
        /// Creates an <see cref="ITagHelperOptionsCollection"/> which can be used to add options pertaining to
        /// <see cref="ITagHelper"/>s to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/> instance this method extends.</param>
        /// <returns>The <see cref="ITagHelperOptionsCollection"/>.</returns>
        public static ITagHelperOptionsCollection ConfigureTagHelpers(
            [NotNull] this IServiceCollection serviceCollection)
        {
            return new TagHelperOptionsCollection(serviceCollection);
        }
    }
}