// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace ValueProvidersWebSite
{
    public class CustomValueProviderFactory : IValueProviderFactory
    {
        public IValueProvider GetValueProvider(ValueProviderFactoryContext context)
        {
            if (context.HttpContext.Request.Path.Value.Contains("TestValueProvider"))
            {
                return new CustomValueProvider();
            }

            return null;
        }

        private class CustomValueProvider : IValueProvider
        {
            public Task<bool> ContainsPrefixAsync(string prefix)
            {
                var result = string.Equals(prefix, "test", StringComparison.OrdinalIgnoreCase);
                return Task.FromResult(result);
            }

            public Task<ValueProviderResult> GetValueAsync(string key)
            {
                var value = "custom-value-provider-value";
                var result = new ValueProviderResult(value, value, CultureInfo.CurrentCulture);
                return Task.FromResult(result);
            }
        }
    }
}