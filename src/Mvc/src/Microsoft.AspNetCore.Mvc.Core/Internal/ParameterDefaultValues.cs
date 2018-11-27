// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public static class ParameterDefaultValues
    {
        public static object[] GetParameterDefaultValues(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            var parameters = methodInfo.GetParameters();
            var values = new object[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                values[i] = GetParameterDefaultValue(parameters[i]);
            }

            return values;
        }

        private static object GetParameterDefaultValue(ParameterInfo parameterInfo)
        {
            if (!ParameterDefaultValue.TryGetDefaultValue(parameterInfo, out var defaultValue))
            {
                var defaultValueAttribute = parameterInfo.GetCustomAttribute<DefaultValueAttribute>(inherit: false);
                defaultValue = defaultValueAttribute?.Value;

                if (defaultValue == null && parameterInfo.ParameterType.IsValueType)
                {
                    defaultValue = Activator.CreateInstance(parameterInfo.ParameterType);
                }
            }
            return defaultValue;
        }
    }
}
