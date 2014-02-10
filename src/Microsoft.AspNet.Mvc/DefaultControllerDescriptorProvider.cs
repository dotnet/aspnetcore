using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultControllerDescriptorProvider : IControllerDescriptorProvider
    {
        private readonly ControllerAssemblyProvider _controllerAssemblyProvider;

        public IReadOnlyDictionary<string, IEnumerable<ControllerDescriptor>> Controllers { get; protected set; }

        public void FinalizeSetup()
        {
            Controllers = ScanAppDomain();
        }

        public DefaultControllerDescriptorProvider(ControllerAssemblyProvider controllerAssemblyProvider)
        {
            if (controllerAssemblyProvider == null)
            {
                throw new ArgumentNullException("controllerAssemblyProvider");
            }

            _controllerAssemblyProvider = controllerAssemblyProvider;
        }

        public IEnumerable<ControllerDescriptor> GetControllers(string controllerName)
        {
            if (!controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            {
                controllerName += "Controller";
            }

            if (Controllers == null)
            {
                throw new InvalidOperationException("Finalizing the setup must happen prior to accessing controllers");
            }

            IEnumerable<ControllerDescriptor> descriptors = null;

            if (Controllers.TryGetValue(controllerName, out descriptors))
            {
                return descriptors;
            }

            return Enumerable.Empty<ControllerDescriptor>();
        }

        public Dictionary<string, IEnumerable<ControllerDescriptor>> ScanAppDomain()
        {
            var dictionary = new Dictionary<string, IEnumerable<ControllerDescriptor>>(StringComparer.Ordinal);

            foreach (var assembly in _controllerAssemblyProvider.Assemblies)
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
    }
}
