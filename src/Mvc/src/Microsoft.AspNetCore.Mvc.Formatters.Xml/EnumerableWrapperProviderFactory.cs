// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
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
        public EnumerableWrapperProviderFactory(IEnumerable<IWrapperProviderFactory> wrapperProviderFactories)
        {
            if (wrapperProviderFactories == null)
            {
                throw new ArgumentNullException(nameof(wrapperProviderFactories));
            }

            _wrapperProviderFactories = wrapperProviderFactories;
        }

        /// <summary>
        /// Gets an <see cref="EnumerableWrapperProvider"/> for the provided context.
        /// </summary>
        /// <param name="context">The <see cref="WrapperProviderContext"/>.</param>
        /// <returns>An instance of <see cref="EnumerableWrapperProvider"/> if the declared type is
        /// an interface and implements <see cref="IEnumerable{T}"/>.</returns>
        public IWrapperProvider GetProvider(WrapperProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.IsSerialization)
            {
                // Example: IEnumerable<SerializableError>
                var declaredType = context.DeclaredType;
                var declaredTypeInfo = declaredType.GetTypeInfo();

                // We only wrap interfaces types(ex: IEnumerable<T>, IQueryable<T>, IList<T> etc.) and not
                // concrete types like List<T>, Collection<T> which implement IEnumerable<T>.
                if (declaredType != null && declaredTypeInfo.IsInterface && declaredTypeInfo.IsGenericType)
                {
                    var enumerableOfT = ClosedGenericMatcher.ExtractGenericInterface(
                        declaredType,
                        typeof(IEnumerable<>));
                    if (enumerableOfT != null)
                    {
                        var elementType = enumerableOfT.GenericTypeArguments[0];
                        var wrapperProviderContext = new WrapperProviderContext(elementType, context.IsSerialization);

                        var elementWrapperProvider =
                            _wrapperProviderFactories.GetWrapperProvider(wrapperProviderContext);

                        return new EnumerableWrapperProvider(enumerableOfT, elementWrapperProvider);
                    }
                }
            }

            return null;
        }
    }
}