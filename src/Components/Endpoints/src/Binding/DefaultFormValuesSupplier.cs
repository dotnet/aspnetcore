// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Components.Binding;
using Microsoft.AspNetCore.Components.Endpoints.Binding;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class DefaultFormValuesSupplier : IFormValueSupplier
{
    private static readonly MethodInfo _method = typeof(DefaultFormValuesSupplier)
            .GetMethod(
                nameof(DeserializeCore),
                BindingFlags.NonPublic | BindingFlags.Static) ??
            throw new InvalidOperationException($"Unable to find method '{nameof(DeserializeCore)}'.");

    private readonly FormDataProvider _formData;
    private readonly FormDataSerializerOptions _options = new();
    private static readonly ConcurrentDictionary<Type, Func<IReadOnlyDictionary<string, StringValues>, FormDataSerializerOptions, string, object>> _cache =
        new();

    public DefaultFormValuesSupplier(FormDataProvider formData)
    {
        _formData = formData;
    }

    public bool CanBind(string formName, Type valueType)
    {
        return _formData.IsFormDataAvailable &&
            string.Equals(formName, _formData.Name, StringComparison.Ordinal) &&
            _options.HasConverter(valueType);
    }

    public bool TryBind(string formName, Type valueType, [NotNullWhen(true)] out object? boundValue)
    {
        // This will func to a proper binder
        if (!CanBind(formName, valueType))
        {
            boundValue = null;
            return false;
        }

        var deserializer = _cache.GetOrAdd(valueType, CreateDeserializer);

        var result = deserializer(_formData.Entries, _options, "value");
        if (result != default)
        {
            // This is not correct, but works for primtive values.
            // Will change the interface when we add support for complex types.
            boundValue = result;
            return true;
        }

        boundValue = valueType.IsValueType ? Activator.CreateInstance(valueType) : null;
        return false;
    }

    private Func<IReadOnlyDictionary<string, StringValues>, FormDataSerializerOptions, string, object> CreateDeserializer(Type type) =>
        _method.MakeGenericMethod(type)
        .CreateDelegate<Func<IReadOnlyDictionary<string, StringValues>, FormDataSerializerOptions, string, object>>();

    private static object? DeserializeCore<T>(IReadOnlyDictionary<string, StringValues> form, FormDataSerializerOptions options, string value)
    {
        // Culture needs to come from the request.
        var reader = new FormDataReader(form, CultureInfo.CurrentCulture);
        reader.PushPrefix(value);
        return FormDataDeserializer.Deserialize<T>(reader, options);
    }

    public bool CanConvertSingleValue(Type type)
    {
        return _options.IsSingleValueConverter(type);
    }
}
