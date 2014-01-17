using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MetadataVirtualPathViewFactory : IVirtualPathViewFactory
    {
        private readonly Dictionary<string, Type> _viewMetadata;

        public MetadataVirtualPathViewFactory(Assembly assembly)
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

        public Task<IView> CreateInstance(string virtualPath)
        {
            Type type;
            IView view = null;
            if (_viewMetadata.TryGetValue(virtualPath, out type))
            {
                view = (IView)Activator.CreateInstance(type);
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
