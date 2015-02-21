// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <inheritdoc />
    public class CompositeValueProviderFactory : ICompositeValueProviderFactory
    {
        private readonly IReadOnlyList<IValueProviderFactory> _valueProviderFactories;

        public CompositeValueProviderFactory(IValueProviderFactoryProvider valueProviderFactoryProvider)
        {
            _valueProviderFactories = valueProviderFactoryProvider.ValueProviderFactories;
        }

        /// <inheritdoc />
        public IValueProvider GetValueProvider([NotNull] ValueProviderFactoryContext context)
        {
            var valueProviders = _valueProviderFactories.Select(factory => factory.GetValueProvider(context))
                                                        .Where(vp => vp != null);

            return new CompositeValueProvider(valueProviders);
        }
    }
}