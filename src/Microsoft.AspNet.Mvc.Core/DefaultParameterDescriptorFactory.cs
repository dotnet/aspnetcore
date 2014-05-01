// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultParameterDescriptorFactory : IParameterDescriptorFactory
    {
        public ParameterDescriptor GetDescriptor(ParameterInfo parameter)
        {
            var isFromBody = IsFromBody(parameter);

            return new ParameterDescriptor
            {
                Name = parameter.Name,
                IsOptional = parameter.IsOptional || parameter.HasDefaultValue,
                ParameterBindingInfo = isFromBody ? null : GetParameterBindingInfo(parameter),
                BodyParameterInfo = isFromBody ? GetBodyParameterInfo(parameter) : null
            };
        }
        public virtual bool IsFromBody(ParameterInfo parameter)
        {
            return parameter.GetCustomAttribute<FromBodyAttribute>() != null;
        }

        private ParameterBindingInfo GetParameterBindingInfo(ParameterInfo parameter)
        {
            return new ParameterBindingInfo(parameter.Name, parameter.ParameterType);
        }

        private BodyParameterInfo GetBodyParameterInfo(ParameterInfo parameter)
        {
            return new BodyParameterInfo(parameter.ParameterType);
        }
    }
}
