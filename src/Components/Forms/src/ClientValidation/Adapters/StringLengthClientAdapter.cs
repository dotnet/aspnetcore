// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.AspNetCore.Components.Forms;

internal sealed class StringLengthClientAdapter(StringLengthAttribute attribute) : IClientValidationAdapter
{
    public void AddClientValidation(in ClientValidationContext context, string errorMessage)
    {
        context.MergeAttribute("data-val", "true");
        context.MergeAttribute("data-val-length", errorMessage);

        if (attribute.MaximumLength != int.MaxValue)
        {
            context.MergeAttribute("data-val-length-max",
                attribute.MaximumLength.ToString(CultureInfo.InvariantCulture));
        }

        if (attribute.MinimumLength != 0)
        {
            context.MergeAttribute("data-val-length-min",
                attribute.MinimumLength.ToString(CultureInfo.InvariantCulture));
        }
    }
}
