using System;

namespace Microsoft.AspNet.Mvc
{
    public class ParameterBindingInfo
    {
        public ParameterBindingInfo(string prefix, Type parameterType)
        {
            Prefix = prefix;
            ParameterType = parameterType;
        }

        public string Prefix { get; private set; }

        public Type ParameterType { get; private set; }
    }
}
