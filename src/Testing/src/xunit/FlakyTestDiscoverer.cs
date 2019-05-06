using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing.xunit
{
    public class FlakyTestDiscoverer : ITraitDiscoverer
    {
        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            if (traitAttribute is ReflectionAttributeInfo attribute && attribute.Attribute is FlakyAttribute flakyAttribute)
            {
                return GetTraitsCore(flakyAttribute);
            }
            else
            {
                throw new InvalidOperationException("The 'Flaky' attribute is only supported via reflection.");
            }
        }

        private IEnumerable<KeyValuePair<string, string>> GetTraitsCore(FlakyAttribute attribute)
        {
            if (attribute.Filters.Count > 0)
            {
                foreach (var filter in attribute.Filters)
                {
                    yield return new KeyValuePair<string, string>($"Flaky:{filter}", "true");
                }
            }
            else
            {
                yield return new KeyValuePair<string, string>($"Flaky:All", "true");
            }
        }
    }
}
