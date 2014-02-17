using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultControllerDescriptorFactory  : IControllerDescriptorFactory
    {
        public ControllerDescriptor CreateControllerDescriptor(TypeInfo typeInfo)
        {
            return new ControllerDescriptor(typeInfo);
        }
    }
}
