// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal sealed class ActionResultTypeMapper : IActionResultTypeMapper
{
    public Type GetResultDataType(Type returnType)
    {
        ArgumentNullException.ThrowIfNull(returnType);

        if (returnType.IsGenericType &&
            returnType.GetGenericTypeDefinition() == typeof(ActionResult<>))
        {
            return returnType.GetGenericArguments()[0];
        }

        return returnType;
    }

    public IActionResult Convert(object? value, Type returnType)
    {
        ArgumentNullException.ThrowIfNull(returnType);

        if (value is IConvertToActionResult converter)
        {
            return converter.Convert();
        }

        if (value is IResult httpResult)
        {
            return new HttpActionResult(httpResult);
        }

        return new ObjectResult(value)
        {
            DeclaredType = returnType,
        };
    }
}
