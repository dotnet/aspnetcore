// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Validation;

internal sealed class ValidatableTypeComparer : IEqualityComparer<ValidatableType?>
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

        return x.TypeFQN.Equals(y.TypeFQN, StringComparison.Ordinal);
    }

    public int GetHashCode(ValidatableType? obj)
        => obj?.TypeFQN is null ? 0 : StringComparer.Ordinal.GetHashCode(obj.TypeFQN);
}
