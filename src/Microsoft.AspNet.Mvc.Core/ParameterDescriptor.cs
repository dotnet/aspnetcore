using System;

namespace Microsoft.AspNet.Mvc
{
    public class ParameterDescriptor
    {
        public string Name { get; set; }

        public bool IsOptional { get; set; }

        public ParameterBindingInfo ParameterBindingInfo { get; set; }

        public BodyParameterInfo BodyParameterInfo { get; set; }
    }
}
