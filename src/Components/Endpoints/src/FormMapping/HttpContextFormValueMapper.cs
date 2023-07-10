// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class HttpContextFormValueMapper : IFormValueMapper
{
    private readonly HttpContextFormDataProvider _formData;
    private readonly FormDataMapperOptions _options = new();
    private static readonly ConcurrentDictionary<Type, FormValueSupplier> _cache = new();

    public HttpContextFormValueMapper(HttpContextFormDataProvider formData)
    {
        _formData = formData;
    }

    public bool CanMap(Type valueType, string? formName = null)
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

    public void Map(FormValueMappingContext context)
    {
        // This will func to a proper binder
        if (!CanMap(context.ValueType, context.FormName))
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
        public abstract void Deserialize(
            FormValueMappingContext context,
            FormDataMapperOptions options,
            IReadOnlyDictionary<string, StringValues> form);
    }

    internal class FormValueSupplier<T> : FormValueSupplier
    {
        public override void Deserialize(
            FormValueMappingContext context,
            FormDataMapperOptions options,
            IReadOnlyDictionary<string, StringValues> form)
        {
            if (form.Count == 0)
            {
                return;
            }

            char[]? buffer = null;
            try
            {
                var dictionary = new Dictionary<FormKey, StringValues>();
                foreach (var (key, value) in form)
                {
                    dictionary.Add(new FormKey(key.AsMemory()), value);
                }
                buffer = ArrayPool<char>.Shared.Rent(options.MaxKeyBufferSize);

                var reader = new FormDataReader(
                    dictionary,
                    options.UseCurrentCulture ? CultureInfo.CurrentCulture : CultureInfo.InvariantCulture,
                    buffer.AsMemory(0, options.MaxKeyBufferSize))
                {
                    ErrorHandler = context.OnError,
                    AttachInstanceToErrorsHandler = context.MapErrorToContainer
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
