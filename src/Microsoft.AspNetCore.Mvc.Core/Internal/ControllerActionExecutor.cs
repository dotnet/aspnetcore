// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public static class ControllerActionExecutor
    {
        public static object[] PrepareArguments(
            IDictionary<string, object> actionParameters,
            ObjectMethodExecutor actionMethodExecutor)
        {
            var declaredParameterInfos = actionMethodExecutor.MethodParameters;
            var count = declaredParameterInfos.Length;
            if (count == 0)
            {
                return null;
            }

            var arguments = new object[count];
            for (var index = 0; index < count; index++)
            {
                var parameterInfo = declaredParameterInfos[index];
                object value;

                if (!actionParameters.TryGetValue(parameterInfo.Name, out value))
                {
                    value = actionMethodExecutor.GetDefaultValueForParameter(index);
                }

                arguments[index] = value;
            }

            return arguments;
        }
    }
}