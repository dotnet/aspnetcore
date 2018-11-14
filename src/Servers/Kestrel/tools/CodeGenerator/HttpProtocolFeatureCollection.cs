// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeGenerator
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class HttpProtocolFeatureCollection
    {
        static string Each<T>(IEnumerable<T> values, Func<T, string> formatter)
        {
            return values.Select(formatter).Aggregate((a, b) => a + b);
        }

        public class KnownFeature
        {
            public string Name;
            public int Index;
        }

        public static string GeneratedFile(string className)
        {
            var alwaysFeatures = new[]
            {
                "IHttpRequestFeature",
                "IHttpResponseFeature",
                "IHttpRequestIdentifierFeature",
                "IServiceProvidersFeature",
                "IHttpRequestLifetimeFeature",
                "IHttpConnectionFeature",
            };

            var commonFeatures = new[]
            {
                "IHttpAuthenticationFeature",
                "IQueryFeature",
                "IFormFeature",
            };

            var sometimesFeatures = new[]
            {
                "IHttpUpgradeFeature",
                "IHttp2StreamIdFeature",
                "IResponseCookiesFeature",
                "IItemsFeature",
                "ITlsConnectionFeature",
                "IHttpWebSocketFeature",
                "ISessionFeature",
                "IHttpMaxRequestBodySizeFeature",
                "IHttpMinRequestBodyDataRateFeature",
                "IHttpMinResponseDataRateFeature",
                "IHttpBodyControlFeature",
            };

            var rareFeatures = new[]
            {
                "IHttpSendFileFeature",
            };

            var allFeatures = alwaysFeatures
                .Concat(commonFeatures)
                .Concat(sometimesFeatures)
                .Concat(rareFeatures)
                .Select((type, index) => new KnownFeature
                {
                    Name = type,
                    Index = index
                });

            // NOTE: This list MUST always match the set of feature interfaces implemented by HttpProtocol.
            // See also: src/Kestrel/Http/HttpProtocol.FeatureCollection.cs
            var implementedFeatures = new[]
            {
                "IHttpRequestFeature",
                "IHttpResponseFeature",
                "IHttpRequestIdentifierFeature",
                "IHttpRequestLifetimeFeature",
                "IHttpConnectionFeature",
                "IHttpMaxRequestBodySizeFeature",
                "IHttpMinRequestBodyDataRateFeature",
                "IHttpMinResponseDataRateFeature",
                "IHttpBodyControlFeature",
            };

            return $@"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{{
    public partial class {className}
    {{{Each(allFeatures, feature => $@"
        private static readonly Type {feature.Name}Type = typeof({feature.Name});")}
{Each(allFeatures, feature => $@"
        private object _current{feature.Name};")}

        private void FastReset()
        {{{Each(implementedFeatures, feature => $@"
            _current{feature} = this;")}
            {Each(allFeatures.Where(f => !implementedFeatures.Contains(f.Name)), feature => $@"
            _current{feature.Name} = null;")}
        }}

        object IFeatureCollection.this[Type key]
        {{
            get
            {{
                object feature = null;{Each(allFeatures, feature => $@"
                {(feature.Index != 0 ? "else " : "")}if (key == {feature.Name}Type)
                {{
                    feature = _current{feature.Name};
                }}")}
                else if (MaybeExtra != null)
                {{
                    feature = ExtraFeatureGet(key);
                }}

                return feature ?? ConnectionFeatures[key];
            }}

            set
            {{
                _featureRevision++;
                {Each(allFeatures, feature => $@"
                {(feature.Index != 0 ? "else " : "")}if (key == {feature.Name}Type)
                {{
                    _current{feature.Name} = value;
                }}")}
                else
                {{
                    ExtraFeatureSet(key, value);
                }}
            }}
        }}

        void IFeatureCollection.Set<TFeature>(TFeature feature) 
        {{
            _featureRevision++;{Each(allFeatures, feature => $@"
            {(feature.Index != 0 ? "else " : "")}if (typeof(TFeature) == typeof({feature.Name}))
            {{
                _current{feature.Name} = feature;
            }}")}
            else
            {{
                ExtraFeatureSet(typeof(TFeature), feature);
            }}
        }}

        TFeature IFeatureCollection.Get<TFeature>()
        {{
            TFeature feature = default;{Each(allFeatures, feature => $@"
            {(feature.Index != 0 ? "else " : "")}if (typeof(TFeature) == typeof({feature.Name}))
            {{
                feature = (TFeature)_current{feature.Name};
            }}")}
            else if (MaybeExtra != null)
            {{
                feature = (TFeature)(ExtraFeatureGet(typeof(TFeature)));
            }}
            
            if (feature == null)
            {{
                feature = ConnectionFeatures.Get<TFeature>();
            }}

            return feature;
        }}

        private IEnumerable<KeyValuePair<Type, object>> FastEnumerable()
        {{{Each(allFeatures, feature => $@"
            if (_current{feature.Name} != null)
            {{
                yield return new KeyValuePair<Type, object>({feature.Name}Type, _current{feature.Name} as {feature.Name});
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
