using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultControllerCache : ControllerCache, IFinalizeSetup
    {
        private readonly SkipAssemblies _skipAssemblies;

        public IReadOnlyDictionary<string, IEnumerable<ControllerDescriptor>> Controllers { get; protected set; }

        public virtual void FinalizeSetup()
        {
            Controllers = ScanAppDomain();
        }

        public DefaultControllerCache(SkipAssemblies skipAssemblies)
        {
            _skipAssemblies = skipAssemblies ?? new SkipNoAssemblies();
        }

        public override IEnumerable<ControllerDescriptor> GetController(string controllerName)
        {
            if (Controllers == null)
            {
                throw new InvalidOperationException("Finalizing the setup must happen prior to accessing controllers");
            }

            IEnumerable<ControllerDescriptor> descriptors = null;

            if (Controllers.TryGetValue(controllerName, out descriptors))
            {
                return descriptors;
            }

            return null;
        }

        public Dictionary<string, IEnumerable<ControllerDescriptor>> ScanAppDomain()
        {
            var dictionary = new Dictionary<string, IEnumerable<ControllerDescriptor>>(StringComparer.Ordinal);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(AllowAssembly))
            {
                foreach (var type in assembly.DefinedTypes.Where(IsController).Select(info => info.AsType()))
                {
                    var descriptor = new ControllerDescriptor(type, assembly);

                    IEnumerable<ControllerDescriptor> controllerDescriptors;
                    if (!dictionary.TryGetValue(type.Name, out controllerDescriptors))
                    {
                        controllerDescriptors = new List<ControllerDescriptor>();
                        dictionary.Add(descriptor.ControllerName, controllerDescriptors);
                    }

                    ((List<ControllerDescriptor>)controllerDescriptors).Add(descriptor);
                }
            }

            return dictionary;
        }

        public virtual bool IsController(TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException("typeInfo");
            }

            bool validController = typeInfo.IsClass &&
                              !typeInfo.IsAbstract &&
                              !typeInfo.ContainsGenericParameters;

            validController = validController && typeInfo.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase);

            return validController;
        }

        private bool AllowAssembly(Assembly assembly)
        {
            return !_skipAssemblies.Skip(assembly, SkipAssemblies.ControllerDiscoveryScope);
        }
    }
}
