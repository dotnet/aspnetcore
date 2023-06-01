// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Binding;
using Microsoft.AspNetCore.Components.Forms;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class DefaultFormValuesSupplier : IFormValueSupplier
{
    private readonly FormDataProvider _formData;

    public DefaultFormValuesSupplier(FormDataProvider formData)
    {
        _formData = formData;
    }

    public bool CanBind(string formName, Type valueType)
    {
        return _formData.IsFormDataAvailable &&
            string.Equals(formName, _formData.Name, StringComparison.Ordinal) &&
            valueType == typeof(string);
    }

    public bool TryBind(string formName, Type valueType, [NotNullWhen(true)] out object? boundValue)
    {
        // This will delegate to a proper binder
        if (!CanBind(formName, valueType))
        {
            boundValue = null;
            return false;
        }

        if (!_formData.Entries.TryGetValue("value", out var rawValue) || rawValue.Count != 1)
        {
            boundValue = null;
            return false;
        }

        var valueAsString = rawValue.ToString();

        if (valueType == typeof(string))
        {
            boundValue = valueAsString;
            return true;
        }

        boundValue = null;
        return false;
    }
}
