// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace BasicWebSite.ValueProviders;

public class CustomValueProviderFactory : IValueProviderFactory
{
    public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
    {
        context.ValueProviders.Add(new CustomValueProvider(context));
        return Task.CompletedTask;
    }

    private class CustomValueProvider : IValueProvider
    {
        private static readonly Dictionary<string, Func<ValueProviderFactoryContext, StringValues>> Values = new()
        {
            { "customValueProviderDisplayName", context => context.ActionContext.ActionDescriptor.DisplayName },
            { "customValueProviderIntValues", _ => new []{ null, "42", "100", null, "200" } },
            { "customValueProviderNullableIntValues", _ => new []{ null, "42", "", "100", null, "200" } },
            { "customValueProviderStringValues", _ => new []{ null, "foo", "", "bar", null, "baz" } },
        };

        private readonly ValueProviderFactoryContext _context;

        public CustomValueProvider(ValueProviderFactoryContext context)
        {
            _context = context;
        }

        public bool ContainsPrefix(string prefix) => Values.ContainsKey(prefix);

        public ValueProviderResult GetValue(string key)
        {
            if (Values.TryGetValue(key, out var fn))
            {
                return new ValueProviderResult(fn(_context));
            }
            return ValueProviderResult.None;
        }
    }
}
