using System;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public class ControllerDescriptor
    {
        public ControllerDescriptor(TypeInfo controllerTypeInfo)
        {
            if (controllerTypeInfo == null)
            {
                throw new ArgumentNullException("controllerTypeInfo");
            }

            ControllerTypeInfo = controllerTypeInfo;

            Name = controllerTypeInfo.Name.EndsWith("Controller", StringComparison.Ordinal)
                 ? controllerTypeInfo.Name.Substring(0, controllerTypeInfo.Name.Length - "Controller".Length)
                 : controllerTypeInfo.Name;
        }

        public string Name { get; private set; }

        public TypeInfo ControllerTypeInfo { get; private set; }
    }
}
