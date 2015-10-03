using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http.Features;
using Microsoft.Dnx.Compilation.CSharp;
using Microsoft.AspNet.Http.Features.Internal;
using Microsoft.AspNet.Http.Features.Authentication;

namespace Microsoft.AspNet.Server.Kestrel.GeneratedCode
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class FrameFeatureCollection : ICompileModule
    {
        static string Each<T>(IEnumerable<T> values, Func<T, string> formatter)
        {
            return values.Select(formatter).Aggregate((a, b) => a + b);
        }

        public virtual void BeforeCompile(BeforeCompileContext context)
        {
            var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(GeneratedFile());
            context.Compilation = context.Compilation.AddSyntaxTrees(syntaxTree);
        }

        public static string GeneratedFile()
        {
            var commonFeatures = new[]
            {
                typeof(IHttpRequestFeature),
                typeof(IHttpResponseFeature),
                typeof(IHttpRequestIdentifierFeature),
                typeof(IHttpSendFileFeature),
                typeof(IServiceProvidersFeature),
                typeof(IHttpAuthenticationFeature),
                typeof(IHttpRequestLifetimeFeature),
                typeof(IQueryFeature),
                typeof(IFormFeature),
                typeof(IResponseCookiesFeature),
                typeof(IItemsFeature),
                typeof(IHttpConnectionFeature),
                typeof(ITlsConnectionFeature),
                typeof(IHttpUpgradeFeature),
                typeof(IHttpWebSocketFeature),
                typeof(ISessionFeature),
            };

            return $@"
using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Server.Kestrel.Http 
{{
    public partial class Frame
    {{
        {Each(commonFeatures.Select((feature, index) => new { feature, index }), entry => $@"
        private const long flag{entry.feature.Name} = {1 << entry.index};")}

        {Each(commonFeatures, feature => $@"
        private static readonly Type {feature.Name}Type = typeof(global::{feature.FullName});")}

        private long _featureOverridenFlags = 0L;

        private void FastReset()
        {{
            _featureOverridenFlags = 0L;
        }}

        private object FastFeatureGet(Type key)
        {{{Each(commonFeatures, feature => $@"
            if (key == {feature.Name}Type)
            {{
                if ((_featureOverridenFlags & flag{feature.Name}) == 0L)
                {{
                    return this;
                }}
                return SlowFeatureGet(key);
            }}")}
            return  SlowFeatureGet(key);
        }}

        private object SlowFeatureGet(Type key)
        {{
            object feature = null;
            if (MaybeExtra?.TryGetValue(key, out feature) ?? false) 
            {{
                return feature;
            }}
            return null;
        }}

        private void FastFeatureSetInner(long flag, Type key, object feature)
        {{
            Extra[key] = feature;

            // Altering only an individual bit of the long
            // so need to make sure other concurrent changes are not overridden
            // in a lock-free manner

            long currentFeatureFlags;
            long updatedFeatureFlags;
            do
            {{
                currentFeatureFlags = _featureOverridenFlags;
                updatedFeatureFlags = currentFeatureFlags | flag;
            }} while (System.Threading.Interlocked.CompareExchange(ref _featureOverridenFlags, updatedFeatureFlags, currentFeatureFlags) != currentFeatureFlags);

            System.Threading.Interlocked.Increment(ref _featureRevision);
        }}

        private void FastFeatureSet(Type key, object feature)
        {{{Each(commonFeatures, feature => $@"
            if (key == {feature.Name}Type)
            {{
                FastFeatureSetInner(flag{feature.Name}, key, feature);
                return;
            }}")}
            Extra[key] = feature;
        }}

        private IEnumerable<KeyValuePair<Type, object>> FastEnumerable()
        {{{Each(commonFeatures, feature => $@"
            if ((_featureOverridenFlags & flag{feature.Name}) == 0L)
            {{
                yield return new KeyValuePair<Type, object>({feature.Name}Type, this as global::{feature.FullName});
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

        public virtual void AfterCompile(AfterCompileContext context)
        {
        }
    }
}
