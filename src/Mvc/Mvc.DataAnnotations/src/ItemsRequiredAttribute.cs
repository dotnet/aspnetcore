// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

internal class ItemsRequiredAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is System.Collections.IEnumerable list)
        {
            foreach (var item in list)
            {
                if (item == null)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
