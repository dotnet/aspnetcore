using System;
using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

// Do not change this namespace without changing the usage in FlakyAttribute
namespace Microsoft.AspNetCore.Testing
{
    public class FlakyTraitDiscoverer : ITraitDiscoverer
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
