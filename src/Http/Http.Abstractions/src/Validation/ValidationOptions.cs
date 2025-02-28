// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.AspNetCore.Http.Validation;

public class ValidationOptions
{
    public IList<IValidatableInfoResolver> Resolvers { get; } = [];
    public int MaxDepth { get; set; } = 32;

    public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out ValidatableTypeInfo? validatableTypeInfo)
    {
        foreach (var resolver in Resolvers)
        {
            validatableTypeInfo = resolver.GetValidatableTypeInfo(type);
            if (validatableTypeInfo is not null)
            {
                return true;
            }
        }

        validatableTypeInfo = null;
        return false;
    }

    public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out ValidatableParameterInfo? validatableParameterInfo)
    {
        foreach (var resolver in Resolvers)
        {
            validatableParameterInfo = resolver.GetValidatableParameterInfo(parameterInfo);
            if (validatableParameterInfo is not null)
            {
                return true;
            }
        }

        validatableParameterInfo = null;
        return false;
    }
}
