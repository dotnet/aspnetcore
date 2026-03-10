// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Components.Forms;

internal sealed class RegexClientAdapter(RegularExpressionAttribute attribute) : IClientValidationAdapter
{
    public void AddClientValidation(in ClientValidationContext context, string errorMessage)
    {
        context.MergeAttribute("data-val", "true");
        context.MergeAttribute("data-val-regex", errorMessage);
        context.MergeAttribute("data-val-regex-pattern", attribute.Pattern);
    }
}
