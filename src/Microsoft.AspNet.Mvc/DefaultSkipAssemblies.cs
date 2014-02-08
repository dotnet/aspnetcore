using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultSkipAssemblies : SkipAssemblies
    {
        private HashSet<string> _hash = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public DefaultSkipAssemblies(IEnumerable<string> assemblyNames)
        {
            InitializeHash(assemblyNames);
        }

        public DefaultSkipAssemblies()
        {
#if NET45
            InitializeHash(@"
klr.net45.managed
Microsoft.Net.Runtime.Interfaces
klr.host
System
System.Core
System.Configuration
System.Xml
Microsoft.Net.ApplicationHost
Microsoft.Net.Runtime
Newtonsoft.Json
System.Numerics
System.ComponentModel.DataAnnotations
System.Runtime.Serialization
System.Xml.Linq
System.Data
Microsoft.CodeAnalysis
System.Collections.Immutable
System.Runtime
Microsoft.CodeAnalysis.CSharp
System.IO.Compression
Microsoft.AspNet.FileSystems
Microsoft.AspNet.Abstractions
Microsoft.AspNet.DependencyInjection
Microsoft.AspNet.Razor
Newtonsoft.Json
System.Linq
System.Collections
System.Runtime.Extensions
System.Threading
System.Reflection.Metadata.Ecma335
Microsoft.AspNet.Mvc.ModelBinding
Microsoft.AspNet.Mvc.Rendering
Microsoft.AspNet.Mvc
Microsoft.AspNet.Mvc.Razor.Host
Microsoft.AspNet.Mvc.Razor
Microsoft.AspNet.Mvc.Startup
Owin
Microsoft.Owin
Microsoft.Owin.Diagnostics
Microsoft.Owin.Hosting
Microsoft.Owin.Host.HttpListener
Microsoft.AspNet.AppBuilderSupport
Anonymously Hosted DynamicMethods Assembly
Microsoft.AspNet.PipelineCore
Microsoft.AspNet.FeatureModel
mscorlib
klr.net45.managed
Microsoft.Net.Runtime.Interfaces
klr.host
System
System.Core
System.Configuration
System.Xml
Microsoft.Net.ApplicationHost
Microsoft.Net.Runtime
Newtonsoft.Json
System.Numerics
System.ComponentModel.DataAnnotations
System.Runtime.Serialization
System.Xml.Linq
System.Data
Microsoft.CodeAnalysis
System.Collections.Immutable
System.Runtime
Microsoft.CodeAnalysis.CSharp
System.IO.Compression
Microsoft.AspNet.FileSystems
Microsoft.AspNet.Abstractions
Microsoft.AspNet.DependencyInjection
Microsoft.AspNet.Razor
Newtonsoft.Json
System.Linq
System.Collections
System.Runtime.Extensions
System.Threading
System.Reflection.Metadata.Ecma335
Microsoft.AspNet.Mvc.ModelBinding
Microsoft.AspNet.Mvc.Rendering
Microsoft.AspNet.Mvc
Microsoft.AspNet.Mvc.Razor.Host
Microsoft.AspNet.Mvc.Razor
Microsoft.AspNet.Mvc.Startup
Owin
Microsoft.Owin
Microsoft.Owin.Diag".Split(new char[] { '\r', '\n'}, StringSplitOptions.RemoveEmptyEntries));
#else
#endif
        }

        private void InitializeHash(IEnumerable<string> assemblyNames)
        {
            if (assemblyNames == null)
            {
                throw new ArgumentNullException("assemblyNames");
            }

            foreach (var assemblyName in assemblyNames)
            {
                if (!string.IsNullOrWhiteSpace(assemblyName))
                {
                    _hash.Add(assemblyName);
                }
            }
        }

        public override bool Skip(Assembly assembly, string scope)
        {
            if (scope == null ||
                !string.Equals(scope, SkipAssemblies.ControllerDiscoveryScope, StringComparison.Ordinal))
            {
                return false;
            }

            string name = assembly.GetName().Name;

            bool contains = _hash.Contains(name);

            return contains;
        }
    }
}
