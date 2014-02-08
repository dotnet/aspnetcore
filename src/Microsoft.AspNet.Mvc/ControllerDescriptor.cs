using System;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public class ControllerDescriptor
    {
        public ControllerDescriptor(Type controllerType, Assembly assembly)
        {
            if (controllerType == null)
            {
                throw new ArgumentNullException("controllerType");
            }

            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            ControllerType = controllerType;
            Assembly = assembly;

            ControllerName = controllerType.Name;
            AssemblyName = assembly.GetName().Name;
        }

        public string ControllerName { get; private set; }

        public string AssemblyName { get; private set; }

        public Type ControllerType { get; private set; }

        public Assembly Assembly { get; private set; }
    }
}
