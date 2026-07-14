// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Validation.Tests;

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#nullable enable

public abstract class ValidationTestBase
{
    public static async Task ValidateAsync(IValidatableTypeInfo typeInfo, object? value, ValidateContext context, bool useAsync, CancellationToken cancellationToken)
    {
        if (useAsync)
        {
            await typeInfo.ValidateAsync(value, context, cancellationToken);
        }
        else
        {
            typeInfo.Validate(value, context);
        }
    }

    public static async Task ValidateAsync(IValidatablePropertyInfo propertyInfo, object containingObject, ValidateContext context, bool useAsync, CancellationToken cancellationToken)
    {
        if (useAsync)
        {
            await propertyInfo.ValidateAsync(containingObject, context, cancellationToken);
        }
        else
        {
            propertyInfo.Validate(containingObject, context);
        }
    }

    public static async Task ValidateAsync(IValidatableParameterInfo parameterInfo, object? value, ValidateContext context, bool useAsync, CancellationToken cancellationToken)
    {
        if (useAsync)
        {
            await parameterInfo.ValidateAsync(value, context, cancellationToken);
        }
        else
        {
            parameterInfo.Validate(value, context);
        }
    }
}
