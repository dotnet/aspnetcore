// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Generic adapter for data-type validation attributes (email, URL, phone, credit card)
/// that emit a single <c>data-val-{ruleName}</c> attribute.
/// </summary>
internal sealed class DataTypeClientAdapter(string ruleName) : IClientValidationAdapter
{
    public void AddClientValidation(in ClientValidationContext context, string errorMessage)
    {
        context.MergeAttribute("data-val", "true");
        context.MergeAttribute(ruleName, errorMessage);
    }
}
