// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Components.Binding;
using Microsoft.AspNetCore.Components.Forms;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class DefaultFormBinderProvider : IFormBinderProvider
{
    private readonly FormDataProvider _formData;
    // Note: This won't be implemented this way.
    private Dictionary<string, TryParseInvoker> _cache = new();

    private delegate bool TryParseInvoker(string value, IFormatProvider formatProvider, out object result);

    public DefaultFormBinderProvider(FormDataProvider formData)
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

        var iParsable = ClosedGenericMatcher.ExtractGenericInterface(typeof(IParsable<>), valueType);
        if (iParsable != null)
        {
            var method = ResolveIParsableTryParse(iParsable, valueType);
            var parameters = new object[3];
            parameters[0] = valueAsString;
            parameters[1] = CultureInfo.CurrentCulture;
            var result = method.Invoke(null, parameters);
            boundValue = parameters[2];

            return result != null && (bool)result;
        }
        boundValue = null;
        return false;
    }

    internal static MethodInfo ResolveIParsableTryParse(Type type, Type parsable)
    {
        var map = type.GetInterfaceMap(parsable);
        for (var i = 0; i < map.TargetMethods.Length; i++)
        {
            var method = map.TargetMethods[i];
            var methodNameStart = method.Name.LastIndexOf('.') + 1;
            if (method.Name.AsSpan()[methodNameStart..].Equals(
                nameof(IParsable<int>.TryParse),
                StringComparison.Ordinal))
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 3 &&
                    parameters[0].ParameterType == typeof(string) &&
                    parameters[1].ParameterType == typeof(IFormatProvider) &&
                    parameters[2].ParameterType == type.MakeByRefType())
                {
                    return method;
                }
            }
        }

        throw new InvalidOperationException($"Unable to resolve TryParse(string s, IFormatProvider, out T result) for type '{type.FullName}'");
    }
}
