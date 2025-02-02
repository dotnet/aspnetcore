// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

internal class ValidatableTypeComparer : IEqualityComparer<ValidatableType?>
{
    public static ValidatableTypeComparer Instance { get; } = new();

    public bool Equals(ValidatableType? x, ValidatableType? y)
    {
        if (x is null && y is null)
        {
            return true;
        }
        if (x is null || y is null)
        {
            return false;
        }
        return x.Name == y.Name;
    }

    public int GetHashCode(ValidatableType? obj)
    {
        return obj?.Name.GetHashCode() ?? 0;
    }
}
