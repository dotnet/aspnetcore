using System;

namespace Microsoft.AspNet.Mvc
{
    public class BodyParameterInfo
    {
        public BodyParameterInfo(Type parameterType)
        {
            ParameterType = parameterType;
        }

        public Type ParameterType { get; private set; }
    }
}

