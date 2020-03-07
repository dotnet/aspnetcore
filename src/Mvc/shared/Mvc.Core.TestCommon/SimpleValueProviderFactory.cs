// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class SimpleValueProviderFactory : IValueProviderFactory
    {
        private readonly IValueProvider _valueProvider;

        public SimpleValueProviderFactory()
        {
            _valueProvider = new SimpleValueProvider();
        }

        public SimpleValueProviderFactory(IValueProvider valueProvider)
        {
            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider));
            }

            _valueProvider = valueProvider;
        }

        public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            context.ValueProviders.Add(_valueProvider);
            return Task.CompletedTask;
        }
    }
}
