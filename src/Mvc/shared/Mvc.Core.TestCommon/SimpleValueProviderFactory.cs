// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


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
