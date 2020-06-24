using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    public class WebAssemblyLazyLoadDefinition
    {
        public WebAssemblyLazyLoadDefinition()
        {
            LazyLoadMappings = new Dictionary<string, IEnumerable<string>>();
        }

        public WebAssemblyLazyLoadDefinition(Dictionary<string, IEnumerable<string>> _lazyLoadMappings)
        {
            LazyLoadMappings = _lazyLoadMappings;
        }

        public void AddRouteDefinition(string route, IEnumerable<string> assemblies)
        {
            LazyLoadMappings[route] = assemblies;
        }

        public IEnumerable<string> GetLazyAssembliesForRoute(string uri)
        {
            IEnumerable<string> assembliesToLoad;

            if (!LazyLoadMappings.TryGetValue(uri, out assembliesToLoad))
            {
                return null;
            }
            return assembliesToLoad;
        }

        public Dictionary<string, IEnumerable<string>> LazyLoadMappings { get; }
    }
}
