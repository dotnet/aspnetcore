// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Xml
{
    /// <summary>
    /// Creates an <see cref="EnumerableWrapperProvider"/> for interface types implementing the 
    /// <see cref="IEnumerable{T}"/> type.
    /// </summary>
    public class EnumerableWrapperProviderFactory : IWrapperProviderFactory
    {
        private readonly IEnumerable<IWrapperProviderFactory> _wrapperProviderFactories;

        /// <summary>
        /// Initializes an <see cref="EnumerableWrapperProviderFactory"/> with a list
        /// <see cref="IWrapperProviderFactory"/>.
        /// </summary>
        /// <param name="wrapperProviderFactories">List of <see cref="IWrapperProviderFactory"/>.</param>
        public EnumerableWrapperProviderFactory([NotNull] IEnumerable<IWrapperProviderFactory> wrapperProviderFactories)
        {
            _wrapperProviderFactories = wrapperProviderFactories;
        }

        /// <summary>
        /// Gets an <see cref="EnumerableWrapperProvider"/> for the provided context.
        /// </summary>
        /// <param name="context">The <see cref="WrapperProviderContext"/>.</param>
        /// <returns>An instance of <see cref="EnumerableWrapperProvider"/> if the declared type is
        /// an interface and implements <see cref="IEnumerable{T}"/>.</returns>
        public IWrapperProvider GetProvider([NotNull] WrapperProviderContext context)
        {
            if (context.IsSerialization)
            {
                // Example: IEnumerable<SerializableError>
                var declaredType = context.DeclaredType;

                // We only wrap interfaces types(ex: IEnumerable<T>, IQueryable<T>, IList<T> etc.) and not
                // concrete types like List<T>, Collection<T> which implement IEnumerable<T>.
                if (declaredType != null && declaredType.IsInterface() && declaredType.IsGenericType())
                {
                    var enumerableOfT = declaredType.ExtractGenericInterface(typeof(IEnumerable<>));
                    if (enumerableOfT != null)
                    {
                        var elementType = enumerableOfT.GetGenericArguments()[0];

                        var wrapperProviderContext = new WrapperProviderContext(
                                                                    elementType,
                                                                    context.IsSerialization);

                        var elementWrapperProvider = _wrapperProviderFactories.GetWrapperProvider(wrapperProviderContext);

                        return new EnumerableWrapperProvider(enumerableOfT, elementWrapperProvider);
                    }
                }
            }

            return null;
        }
    }
}