
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public interface IParameterDescriptorFactory
    {
        ParameterDescriptor GetDescriptor(ParameterInfo parameter);
    }
}
