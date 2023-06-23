// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Components.Binding;
using Microsoft.AspNetCore.Components.Endpoints.Binding;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class DefaultFormValuesSupplier : IFormValueSupplier
{
    private readonly HttpContextFormDataProvider _formData;
    private readonly FormDataMapperOptions _options = new();
    private static readonly ConcurrentDictionary<Type, FormValueSupplier> _cache = new();

    public DefaultFormValuesSupplier(FormDataProvider formData)
    {
        _formData = (HttpContextFormDataProvider)formData;
    }

    public bool CanBind(Type valueType, string? formName = null)
    {
        if (formName == null)
        {
            return _options.ResolveConverter(valueType) != null;
        }
        else
        {
            var result = _formData.IsFormDataAvailable &&
                string.Equals(formName, _formData.Name, StringComparison.Ordinal) &&
                _options.ResolveConverter(valueType) != null;

            return result;
        }
    }

    public void Bind(FormValueSupplierContext context)
    {
        // This will func to a proper binder
        if (!CanBind(context.ValueType, context.FormName))
        {
            context.SetResult(null);
        }

        var deserializer = _cache.GetOrAdd(context.ValueType, CreateDeserializer);
        Debug.Assert(deserializer != null);

        deserializer.Deserialize(context, _options, _formData.Entries);
    }

    private FormValueSupplier CreateDeserializer(Type type) =>
        (FormValueSupplier)Activator.CreateInstance(typeof(FormValueSupplier<>)
        .MakeGenericType(type))!;

    internal abstract class FormValueSupplier
    {
        public abstract void Deserialize(FormValueSupplierContext context, FormDataMapperOptions options, IReadOnlyDictionary<string, StringValues> form);
    }

    internal class FormValueSupplier<T> : FormValueSupplier
    {
        public override void Deserialize(FormValueSupplierContext context, FormDataMapperOptions options, IReadOnlyDictionary<string, StringValues> form)
        {
            if (form.Count == 0)
            {
                return;
            }

            char[]? buffer = null;
            try
            {
                var maxKeyLenght = -1;
                var dictionary = new Dictionary<FormKey, StringValues>();
                foreach (var (key, value) in form)
                {
                    if (key.Length > maxKeyLenght)
                    {
                        maxKeyLenght = key.Length;
                    }
                    dictionary.Add(new FormKey(key.AsMemory()), value);
                }
                buffer = ArrayPool<char>.Shared.Rent(maxKeyLenght);

                // Form values are parsed according to the culture of the request, which is set to the current culture by the localization middleware.
                // Some form input types use the invariant culture when sending the data to the server. For those cases, we'll
                // provide a way to override the culture to use to parse that value.
                var reader = new FormDataReader(dictionary, CultureInfo.CurrentCulture, buffer)
                {
                    ErrorHandler = context.OnError
                };
                reader.PushPrefix(context.ParameterName);
                var result = FormDataMapper.Map<T>(reader, options);
                context.SetResult(result);
            }
            finally
            {
                if (buffer != null)
                {
                    ArrayPool<char>.Shared.Return(buffer);
                }
            }
        }
    }
}
