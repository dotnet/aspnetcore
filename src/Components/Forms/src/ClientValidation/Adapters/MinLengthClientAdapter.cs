// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.AspNetCore.Components.Forms;

internal sealed class MinLengthClientAdapter(MinLengthAttribute attribute) : IClientValidationAdapter
{
    public void AddClientValidation(in ClientValidationContext context, string errorMessage)
    {
        context.MergeAttribute("data-val", "true");
        context.MergeAttribute("data-val-minlength", errorMessage);
        context.MergeAttribute("data-val-minlength-min",
            attribute.Length.ToString(CultureInfo.InvariantCulture));
    }
}
