// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultParameterDescriptorFactory : IParameterDescriptorFactory
    {
        public ParameterDescriptor GetDescriptor(ParameterInfo parameter)
        {
            bool isFromBody = IsFromBody(parameter);

            return new ParameterDescriptor
            {
                Name = parameter.Name,
                IsOptional = parameter.IsOptional,
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
