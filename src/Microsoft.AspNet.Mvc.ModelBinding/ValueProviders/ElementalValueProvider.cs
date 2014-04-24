using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    // Represents a value provider that contains a single value.
    internal sealed class ElementalValueProvider : IValueProvider
    {
        public ElementalValueProvider(string name, object rawValue, CultureInfo culture)
        {
            Name = name;
            RawValue = rawValue;
            Culture = culture;
        }

        public CultureInfo Culture { get; private set; }

        public string Name { get; private set; }

        public object RawValue { get; private set; }

        public Task<bool> ContainsPrefixAsync(string prefix)
        {
            return Task.FromResult(PrefixContainer.IsPrefixMatch(Name, prefix));
        }

        public Task<ValueProviderResult> GetValueAsync(string key)
        {
            var result = string.Equals(key, Name, StringComparison.OrdinalIgnoreCase) ?
                                new ValueProviderResult(RawValue, Convert.ToString(RawValue, Culture), Culture) :
                                null;
            return Task.FromResult(result);
        }
    }
}
