// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.TagHelpers;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="ITagHelperOptionsCollection"/>.
    /// </summary>
    public static class TagHelperOptionsCollectionExtensions
    {
        /// <summary>
        /// Configures options for the <see cref="FormTagHelper"/> from an <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="collection">The <see cref="ITagHelperOptionsCollection"/> instance this method extends.</param>
        /// <param name="configuration">An <see cref="IConfiguration"/> to get the options from.</param>
        /// <returns>The <see cref="ITagHelperOptionsCollection"/>.</returns>
        public static ITagHelperOptionsCollection ConfigureForm(
            [NotNull] this ITagHelperOptionsCollection collection,
            [NotNull] IConfiguration configuration)
        {
            collection.Services.Configure<FormTagHelperOptions>(configuration);

            return collection;
        }

        /// <summary>
        /// Configures options for the <see cref="FormTagHelper"/> using a delegate.
        /// </summary>
        /// <param name="collection">The <see cref="ITagHelperOptionsCollection"/> instance this method extends.</param>
        /// <param name="setupAction">The options setup delegate.</param>
        /// <returns>The <see cref="ITagHelperOptionsCollection"/>.</returns>
        public static ITagHelperOptionsCollection ConfigureForm(
            [NotNull] this ITagHelperOptionsCollection collection,
            [NotNull] Action<FormTagHelperOptions> setupAction)
        {
            collection.Services.Configure(setupAction);

            return collection;
        }
    }
}