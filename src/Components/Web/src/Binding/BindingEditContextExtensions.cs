// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms;

namespace Microsoft.AspNetCore.Components.Binding;

internal static class BindingEditContextExtensions
{
    private static readonly object _convertibleTypesKey = new object();

    public static void SetConvertibleValues(
        this EditContext context,
        ModelBindingContext binding)
    {
        context.Properties[_convertibleTypesKey] = (Predicate<Type>)binding.CanConvert;
    }

    public static Predicate<Type>? GetConvertibleValues(this EditContext context)
    {
        return context.Properties.TryGetValue(_convertibleTypesKey, out var result) ? (Predicate<Type>)result : null;
    }
}
