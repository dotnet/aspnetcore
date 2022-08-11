// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

internal static class FormModelBindingHelper
{
    public const string CultureInvariantFieldName = "__Invariant";

    public static bool InputTypeUsesCultureInvariantFormatting(string? inputType)
        => inputType is "number" or "range";
}
