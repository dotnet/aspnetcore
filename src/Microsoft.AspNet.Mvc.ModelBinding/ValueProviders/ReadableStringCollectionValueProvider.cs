using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ReadableStringCollectionValueProvider : IEnumerableValueProvider
    {
        private readonly CultureInfo _culture;
        private PrefixContainer _prefixContainer;
        private readonly IReadableStringCollection _values;

        /// <summary>
        /// Creates a NameValuePairsProvider wrapping an existing set of key value pairs.
        /// </summary>
        /// <param name="values">The key value pairs to wrap.</param>
        /// <param name="culture">The culture to return with ValueProviderResult instances.</param>
        public ReadableStringCollectionValueProvider(IReadableStringCollection values, CultureInfo culture)
        {
            if (values == null)
            {
                throw Error.ArgumentNull("values");
            }

            _values = values;
            _culture = culture;
        }

        public CultureInfo Culture
        {
            get
            {
                return _culture;
            }
        }

        private PrefixContainer PrefixContainer
        {
            get
            {
                if (_prefixContainer == null)
                {
                    // Initialization race is OK providing data remains read-only and object identity is not significant
                    // TODO: Figure out if we can have IReadableStringCollection expose Keys, Count etc

                    _prefixContainer = new PrefixContainer(_values.Select(v => v.Key).ToArray());
                }
                return _prefixContainer;
            }
        }

        public virtual bool ContainsPrefix(string prefix)
        {
            return PrefixContainer.ContainsPrefix(prefix);
        }

        public virtual IDictionary<string, string> GetKeysFromPrefix(string prefix)
        {
            if (prefix == null)
            {
                throw Error.ArgumentNull("prefix");
            }

            return PrefixContainer.GetKeysFromPrefix(prefix);
        }

        public virtual ValueProviderResult GetValue(string key)
        {
            if (key == null)
            {
                throw Error.ArgumentNull("key");
            }

            IList<string> values = _values.GetValues(key);
            if (values == null)
            {
                return null;
            }
            else if (values.Count == 1)
            {
                var value = (string)values[0];
                return new ValueProviderResult(value, value, _culture);
            }

            return new ValueProviderResult(values, _values.Get(key), _culture);
        }
    }
}
