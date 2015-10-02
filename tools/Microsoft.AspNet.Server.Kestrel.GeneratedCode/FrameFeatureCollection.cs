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
        {Each(commonFeatures, feature => $@"
        private object _current{feature.Name};")}

        private void FastReset()
        {{{Each(commonFeatures, feature => $@"
            _current{feature.Name} = this as global::{feature.FullName};")}
        }}

        private object FastFeatureGet(Type key)
        {{{Each(commonFeatures, feature => $@"
            if (key == typeof(global::{feature.FullName}))
            {{
                return _current{feature.Name};
            }}")}
            object feature = null;
            if (MaybeExtra?.TryGetValue(key, out feature) ?? false) 
            {{
                return feature;
            }}
            return null;
        }}

        private void FastFeatureSet(Type key, object feature)
        {{{Each(commonFeatures, feature => $@"
            if (key == typeof(global::{feature.FullName}))
            {{
                _current{feature.Name} = feature;
                System.Threading.Interlocked.Increment(ref _featureRevision);
                return;
            }}")}
            Extra[key] = feature;
        }}

        private IEnumerable<KeyValuePair<Type, object>> FastEnumerable()
        {{{Each(commonFeatures, feature => $@"
            if (_current{feature.Name} != null)
            {{
                yield return new KeyValuePair<Type, object>(typeof(global::{feature.FullName}), _current{feature.Name});
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
