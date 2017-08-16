// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace CodeGenerator
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class FrameFeatureCollection
    {
        static string Each<T>(IEnumerable<T> values, Func<T, string> formatter)
        {
            return values.Select(formatter).Aggregate((a, b) => a + b);
        }

        public static string GeneratedFile(string className, string namespaceSuffix, IEnumerable<Type> additionalFeatures = null)
        {
            additionalFeatures = additionalFeatures ?? new Type[] { };

            var alwaysFeatures = new[]
            {
                typeof(IHttpRequestFeature),
                typeof(IHttpResponseFeature),
                typeof(IHttpRequestIdentifierFeature),
                typeof(IServiceProvidersFeature),
                typeof(IHttpRequestLifetimeFeature),
                typeof(IHttpConnectionFeature),
            };

            var commonFeatures = new[]
            {
                typeof(IHttpAuthenticationFeature),
                typeof(IQueryFeature),
                typeof(IFormFeature),
            };

            var sometimesFeatures = new[]
            {
                typeof(IHttpUpgradeFeature),
                typeof(IResponseCookiesFeature),
                typeof(IItemsFeature),
                typeof(ITlsConnectionFeature),
                typeof(IHttpWebSocketFeature),
                typeof(ISessionFeature),
                typeof(IHttpMaxRequestBodySizeFeature),
                typeof(IHttpMinRequestBodyDataRateFeature),
                typeof(IHttpMinResponseDataRateFeature),
                typeof(IHttpBodyControlFeature),
            };

            var rareFeatures = new[]
            {
                typeof(IHttpSendFileFeature),
            };

            var allFeatures = alwaysFeatures.Concat(commonFeatures).Concat(sometimesFeatures).Concat(rareFeatures).Concat(additionalFeatures);

            // NOTE: This list MUST always match the set of feature interfaces implemented by Frame.
            // See also: src/Kestrel/Http/Frame.FeatureCollection.cs
            var implementedFeatures = new[]
            {
                typeof(IHttpRequestFeature),
                typeof(IHttpResponseFeature),
                typeof(IHttpUpgradeFeature),
                typeof(IHttpRequestIdentifierFeature),
                typeof(IHttpRequestLifetimeFeature),
                typeof(IHttpConnectionFeature),
                typeof(IHttpMaxRequestBodySizeFeature),
                typeof(IHttpMinRequestBodyDataRateFeature),
                typeof(IHttpMinResponseDataRateFeature),
                typeof(IHttpBodyControlFeature),
            }.Concat(additionalFeatures);

            return $@"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.{namespaceSuffix}
{{
    public partial class {className}
    {{{Each(allFeatures, feature => $@"
        private static readonly Type {feature.Name}Type = typeof(global::{feature.FullName});")}
{Each(allFeatures, feature => $@"
        private object _current{feature.Name};")}

        private void FastReset()
        {{{Each(implementedFeatures, feature => $@"
            _current{feature.Name} = this;")}
            {Each(allFeatures.Where(f => !implementedFeatures.Contains(f)), feature => $@"
            _current{feature.Name} = null;")}
        }}

        internal object FastFeatureGet(Type key)
        {{{Each(allFeatures, feature => $@"
            if (key == {feature.Name}Type)
            {{
                return _current{feature.Name};
            }}")}
            return ExtraFeatureGet(key);
        }}

        internal void FastFeatureSet(Type key, object feature)
        {{
            _featureRevision++;
            {Each(allFeatures, feature => $@"
            if (key == {feature.Name}Type)
            {{
                _current{feature.Name} = feature;
                return;
            }}")};
            ExtraFeatureSet(key, feature);
        }}

        private IEnumerable<KeyValuePair<Type, object>> FastEnumerable()
        {{{Each(allFeatures, feature => $@"
            if (_current{feature.Name} != null)
            {{
                yield return new KeyValuePair<Type, object>({feature.Name}Type, _current{feature.Name} as global::{feature.FullName});
            }}")}

            if (MaybeExtra != null)
            {{
                foreach(var item in MaybeExtra)
                {{
                    yield return item;
                }}
            }}
        }}
    }}
}}
";
        }
    }
}
