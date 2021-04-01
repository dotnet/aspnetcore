// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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