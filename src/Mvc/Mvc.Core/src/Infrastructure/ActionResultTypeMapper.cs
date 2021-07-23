// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    internal class ActionResultTypeMapper : IActionResultTypeMapper
    {
        public Type GetResultDataType(Type returnType)
        {
            if (returnType == null)
            {
                throw new ArgumentNullException(nameof(returnType));
            }

            if (returnType.IsGenericType &&
                returnType.GetGenericTypeDefinition() == typeof(ActionResult<>))
            {
                return returnType.GetGenericArguments()[0];
            }

            return returnType;
        }

        public IActionResult Convert(object? value, Type returnType)
        {
            if (returnType == null)
            {
                throw new ArgumentNullException(nameof(returnType));
            }

            if (value is IConvertToActionResult converter)
            {
                return converter.Convert();
            }

            return new ObjectResult(value)
            {
                DeclaredType = returnType,
            };
        }
    }
}
