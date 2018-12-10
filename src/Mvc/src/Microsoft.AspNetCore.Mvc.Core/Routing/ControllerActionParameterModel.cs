// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Routing
{
    public class ControllerActionParameterModel
    {
        public ControllerActionParameterModel(ParameterInfo parameter)
        {
            if (parameter == null)
            {
                throw new System.ArgumentNullException(nameof(parameter));
            }

            Parameter = parameter;

            Name = parameter.Name;
            ParameterType = parameter.ParameterType;
        }

        public ControllerActionParameterModel(PropertyInfo property)
        {
            if (property == null)
            {
                throw new System.ArgumentNullException(nameof(property));
            }

            Property = property;

            Name = property.Name;
            ParameterType = property.PropertyType;
        }

        public ControllerActionParameterModel(ControllerActionParameterModel other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            BindingInfo = new BindingInfo(other.BindingInfo);
            Name = other.Name;
            Property = other.Property;
            Parameter = other.Parameter;
            ParameterType = other.ParameterType;
        }

        public BindingInfo BindingInfo { get; set; }

        public string Name { get; set; }

        public PropertyInfo Property { get; }

        public ParameterInfo Parameter { get; }

        public Type ParameterType { get; }
    }
}
