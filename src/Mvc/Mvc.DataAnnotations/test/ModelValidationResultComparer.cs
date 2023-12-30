// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

public class ModelValidationResultComparer : IEqualityComparer<ModelValidationResult>
{
    public static readonly ModelValidationResultComparer Instance = new ModelValidationResultComparer();

    private ModelValidationResultComparer()
    {
    }

    public bool Equals(ModelValidationResult x, ModelValidationResult y)
    {
        if (x == null || y == null)
        {
            return x == null && y == null;
        }

        return string.Equals(x.MemberName, y.MemberName, StringComparison.Ordinal) &&
            string.Equals(x.Message, y.Message, StringComparison.Ordinal);
    }

    public int GetHashCode(ModelValidationResult obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return obj.MemberName.GetHashCode();
    }
}
