// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Binding;

namespace Microsoft.AspNetCore.Components.WebView.Services;

internal class WebViewFormValueSupplier : IFormValueSupplier
{
    public bool CanBind(string formName, Type valueType)
    {
        return false;
    }

    public bool CanConvertSingleValue(Type type)
    {
        return false;
    }

    public bool TryBind(string formName, Type valueType, [NotNullWhen(true)] out object? boundValue)
    {
        boundValue = null;
        return false;
    }
}
