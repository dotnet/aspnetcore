// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.AspNetCore.Components.Forms;

internal sealed class RangeClientAdapter(RangeAttribute attribute) : IClientValidationAdapter
{
    public void AddClientValidation(in ClientValidationContext context, string errorMessage)
    {
        context.MergeAttribute("data-val", "true");
        context.MergeAttribute("data-val-range", errorMessage);
        context.MergeAttribute("data-val-range-min",
            Convert.ToString(attribute.Minimum, CultureInfo.InvariantCulture)!);
        context.MergeAttribute("data-val-range-max",
            Convert.ToString(attribute.Maximum, CultureInfo.InvariantCulture)!);
    }
}
