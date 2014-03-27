using System;
using System.Globalization;
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

        public bool ContainsPrefix(string prefix)
        {
            return PrefixContainer.IsPrefixMatch(Name, prefix);
        }

        public ValueProviderResult GetValue(string key)
        {
            return string.Equals(key, Name, StringComparison.OrdinalIgnoreCase)
                       ? new ValueProviderResult(RawValue, Convert.ToString(RawValue, Culture), Culture)
                       : null;
        }
    }
}
