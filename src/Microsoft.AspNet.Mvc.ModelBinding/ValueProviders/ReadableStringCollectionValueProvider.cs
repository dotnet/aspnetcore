using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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
        public ReadableStringCollectionValueProvider([NotNull] IReadableStringCollection values, CultureInfo culture)
        {
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

        public virtual Task<bool> ContainsPrefixAsync(string prefix)
        {
            return Task.FromResult(PrefixContainer.ContainsPrefix(prefix));
        }

        public virtual IDictionary<string, string> GetKeysFromPrefix([NotNull] string prefix)
        {
            return PrefixContainer.GetKeysFromPrefix(prefix);
        }

        public virtual Task<ValueProviderResult> GetValueAsync([NotNull] string key)
        {
            ValueProviderResult result;
            var values = _values.GetValues(key);
            if (values == null)
            {
                result = null;
            }
            else if (values.Count == 1)
            {
                var value = (string)values[0];
                result = new ValueProviderResult(value, value, _culture);
            }
            else
            {
                result = new ValueProviderResult(values, _values.Get(key), _culture);
            }

            return Task.FromResult(result);
        }
    }
}
