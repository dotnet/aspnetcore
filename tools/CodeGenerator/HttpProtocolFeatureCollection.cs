// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
//using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

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

            var allFeatures = alwaysFeatures.Concat(commonFeatures).Concat(sometimesFeatures).Concat(rareFeatures);

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
        private static readonly Type {feature}Type = typeof({feature});")}
{Each(allFeatures, feature => $@"
        private object _current{feature};")}

        private void FastReset()
        {{{Each(implementedFeatures, feature => $@"
            _current{feature} = this;")}
            {Each(allFeatures.Where(f => !implementedFeatures.Contains(f)), feature => $@"
            _current{feature} = null;")}
        }}

        internal object FastFeatureGet(Type key)
        {{{Each(allFeatures, feature => $@"
            if (key == {feature}Type)
            {{
                return _current{feature};
            }}")}
            return ExtraFeatureGet(key);
        }}

        protected void FastFeatureSet(Type key, object feature)
        {{
            _featureRevision++;
            {Each(allFeatures, feature => $@"
            if (key == {feature}Type)
            {{
                _current{feature} = feature;
                return;
            }}")};
            ExtraFeatureSet(key, feature);
        }}

        private IEnumerable<KeyValuePair<Type, object>> FastEnumerable()
        {{{Each(allFeatures, feature => $@"
            if (_current{feature} != null)
            {{
                yield return new KeyValuePair<Type, object>({feature}Type, _current{feature} as {feature});
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
