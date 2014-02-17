using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public interface IControllerDescriptorFactory
    {
        ControllerDescriptor CreateControllerDescriptor(TypeInfo type);
    }
}
