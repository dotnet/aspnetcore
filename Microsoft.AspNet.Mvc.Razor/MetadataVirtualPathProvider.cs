using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MetadataVirtualPathProvider : IVirtualPathFactory
    {
        private readonly Dictionary<string, Type> _viewMetadata;

        public MetadataVirtualPathProvider(Assembly assembly)
        {
            var metadataType = assembly.GetType("ViewMetadata");
            if (metadataType != null)
            {
                object metadata = metadataType.GetProperty("Metadata", BindingFlags.Static | BindingFlags.Public)
                                              .GetValue(obj: null);

                _viewMetadata = new Dictionary<string, Type>((Dictionary<string, Type>)metadata, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                // Code to support precompiled views generated via RazorGenerator
                _viewMetadata = assembly.GetExportedTypes()
                                        .Where(type => typeof(RazorView).IsAssignableFrom(type))
                                        .ToDictionary(type => GetVirtualPath(type), StringComparer.OrdinalIgnoreCase);
            }
        }

        public Task<object> CreateInstance(string virtualPath)
        {
            Type type;
            object view = null;
            if (_viewMetadata.TryGetValue(virtualPath, out type))
            {
                view = (RazorView)Activator.CreateInstance(type);
            }
            return Task.FromResult(view);
        }

        private static string GetVirtualPath(Type type)
        {
            VirtualPathAttribute attribute = type.GetCustomAttribute<VirtualPathAttribute>();
            return attribute.VirtualPath;
        }
    }
}
