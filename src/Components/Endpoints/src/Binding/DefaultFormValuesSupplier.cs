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

internal sealed class DefaultFormValuesSupplier : IFormValueSupplier
{
    private static readonly MethodInfo _method = typeof(DefaultFormValuesSupplier)
            .GetMethod(
                nameof(DeserializeCore),
                BindingFlags.NonPublic | BindingFlags.Static) ??
            throw new InvalidOperationException($"Unable to find method '{nameof(DeserializeCore)}'.");

    private readonly HttpContextFormDataProvider _formData;
    private readonly FormDataMapperOptions _options = new();
    private static readonly ConcurrentDictionary<Type, Func<IReadOnlyDictionary<string, StringValues>, FormDataMapperOptions, string, object>> _cache =
        new();

    public DefaultFormValuesSupplier(FormDataProvider formData)
    {
        _formData = (HttpContextFormDataProvider)formData;
    }

    public bool CanBind(string formName, Type valueType)
    {
        return _formData.IsFormDataAvailable &&
            string.Equals(formName, _formData.Name, StringComparison.Ordinal) &&
            _options.ResolveConverter(valueType) != null;
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

    private Func<IReadOnlyDictionary<string, StringValues>, FormDataMapperOptions, string, object> CreateDeserializer(Type type) =>
        _method.MakeGenericMethod(type)
        .CreateDelegate<Func<IReadOnlyDictionary<string, StringValues>, FormDataMapperOptions, string, object>>();

    private static object? DeserializeCore<T>(IReadOnlyDictionary<string, StringValues> form, FormDataMapperOptions options, string value)
    {
        // Form values are parsed according to the culture of the request, which is set to the current culture by the localization middleware.
        // Some form input types use the invariant culture when sending the data to the server. For those cases, we'll
        // provide a way to override the culture to use to parse that value.
        var reader = new FormDataReader(form, CultureInfo.CurrentCulture);
        reader.PushPrefix(value);
        return FormDataMapper.Map<T>(reader, options);
    }

    public bool CanConvertSingleValue(Type type)
    {
        return _options.IsSingleValueConverter(type);
    }
}
