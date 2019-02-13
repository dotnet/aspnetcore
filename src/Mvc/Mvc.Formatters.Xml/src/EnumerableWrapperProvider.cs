// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    /// <summary>
    /// Provides a <see cref="IWrapperProvider"/> for interface types which implement
    /// <see cref="IEnumerable{T}"/>.
    /// </summary>
    public class EnumerableWrapperProvider : IWrapperProvider
    {
        private readonly IWrapperProvider _wrapperProvider;
        private readonly ConstructorInfo _wrappingTypeConstructor;

        /// <summary>
        /// Initializes an instance of <see cref="EnumerableWrapperProvider"/>.
        /// </summary>
        /// <param name="sourceEnumerableOfT">Type of the original <see cref="IEnumerable{T}" />
        /// that is being wrapped.</param>
        /// <param name="elementWrapperProvider">The <see cref="IWrapperProvider"/> for the element type.
        /// Can be null.</param>
        public EnumerableWrapperProvider(
            Type sourceEnumerableOfT,
            IWrapperProvider elementWrapperProvider)
        {
            if (sourceEnumerableOfT == null)
            {
                throw new ArgumentNullException(nameof(sourceEnumerableOfT));
            }

            var enumerableOfT = ClosedGenericMatcher.ExtractGenericInterface(
                sourceEnumerableOfT,
                typeof(IEnumerable<>));
            if (!sourceEnumerableOfT.GetTypeInfo().IsInterface || enumerableOfT == null)
            {
                throw new ArgumentException(
                    Resources.FormatEnumerableWrapperProvider_InvalidSourceEnumerableOfT(typeof(IEnumerable<>).Name),
                    nameof(sourceEnumerableOfT));
            }

            _wrapperProvider = elementWrapperProvider;

            var declaredElementType = enumerableOfT.GenericTypeArguments[0];
            var wrappedElementType = elementWrapperProvider?.WrappingType ?? declaredElementType;
            WrappingType = typeof(DelegatingEnumerable<,>).MakeGenericType(wrappedElementType, declaredElementType);

            _wrappingTypeConstructor = WrappingType.GetConstructor(new[]
            {
                sourceEnumerableOfT,
                typeof(IWrapperProvider)
            });
        }

        /// <inheritdoc />
        public Type WrappingType
        {
            get;
        }

        /// <inheritdoc />
        public object Wrap(object original)
        {
            if (original == null)
            {
                return null;
            }

            return _wrappingTypeConstructor.Invoke(new[] { original, _wrapperProvider });
        }
    }
}