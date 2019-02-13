// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    /// <summary>
    /// Extension methods for <see cref="IWrapperProviderFactory"/>.
    /// </summary>
    public static class WrapperProviderFactoriesExtensions
    {
        /// <summary>
        /// Gets an instance of <see cref="IWrapperProvider"/> for the supplied
        /// type.
        /// </summary>
        /// <param name="wrapperProviderFactories">A list of <see cref="IWrapperProviderFactory"/>.</param>
        /// <param name="wrapperProviderContext">The <see cref="WrapperProviderContext"/>.</param>
        /// <returns>An instance of <see cref="IWrapperProvider"/> if there is a wrapping provider for the
        /// supplied type, else null.</returns>
        public static IWrapperProvider GetWrapperProvider(
            this IEnumerable<IWrapperProviderFactory> wrapperProviderFactories,
            WrapperProviderContext wrapperProviderContext)
        {
            if (wrapperProviderFactories == null)
            {
                throw new ArgumentNullException(nameof(wrapperProviderFactories));
            }

            if (wrapperProviderContext == null)
            {
                throw new ArgumentNullException(nameof(wrapperProviderContext));
            }

            foreach (var wrapperProviderFactory in wrapperProviderFactories)
            {
                var wrapperProvider = wrapperProviderFactory.GetProvider(wrapperProviderContext);
                if (wrapperProvider != null)
                {
                    return wrapperProvider;
                }
            }

            return null;
        }
    }
}