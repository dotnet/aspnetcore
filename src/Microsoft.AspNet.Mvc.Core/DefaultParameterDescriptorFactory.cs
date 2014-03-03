
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultParameterDescriptorFactory : IParameterDescriptorFactory
    {
        public ParameterDescriptor GetDescriptor(ParameterInfo parameter)
        {
            var bindingInfo = new ParameterBindingInfo()
            {
                IsOptional = parameter.IsOptional,
                IsFromBody = IsFromBody(parameter),
                Prefix = parameter.Name,
            };

            return new ParameterDescriptor()
            {
                Name = parameter.Name,
                Binding = bindingInfo,
            };
        }

        public virtual bool IsFromBody(ParameterInfo parameter)
        {
            // Assume for now everything is read from value providers
            return false;
        }
    }
}
