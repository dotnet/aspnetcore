// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace ValueProvidersWebSite
{
    public class CustomValueProviderFactory : IValueProviderFactory
    {
        public Task<IValueProvider> GetValueProviderAsync(ActionContext context)
        {
            if (context.HttpContext.Request.Path.Value.Contains("TestValueProvider"))
            {
                return Task.FromResult<IValueProvider>(new CustomValueProvider());
            }

            return Task.FromResult<IValueProvider>(null);
        }

        private class CustomValueProvider : IValueProvider
        {
            public bool ContainsPrefix(string prefix)
            {
                return string.Equals(prefix, "test", StringComparison.OrdinalIgnoreCase);
            }

            public ValueProviderResult GetValue(string key)
            {
                var value = "custom-value-provider-value";
                return new ValueProviderResult(value, CultureInfo.CurrentCulture);
            }
        }
    }
}