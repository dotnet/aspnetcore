// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping;
using Microsoft.AspNetCore.Components.Forms.Mapping;

namespace Microsoft.AspNetCore.Components.Mcp.FormMapping;

internal sealed class McpFormValueMapper(McpFormDataProvider formData, FormDataMapperOptions options) : IFormValueMapper
{
    private readonly Dictionary<Type, FormValueSupplier> _suppliers = new();

    public bool CanMap(Type valueType, string scopeName, string? formName)
    {
        if (formData.HandlerName is not { } handlerName
            || !MatchesScope(handlerName, scopeName, out var incomingFormName))
        {
            return false;
        }

        if (formName is not null && !incomingFormName.Equals(formName, StringComparison.Ordinal))
        {
            return false;
        }

        return options.ResolveConverter(valueType) is not null;
    }

    public void Map(FormValueMappingContext context)
    {
        if (!CanMap(context.ValueType, context.AcceptMappingScopeName, context.AcceptFormName))
        {
            context.SetResult(null);
            return;
        }

        if (!_suppliers.TryGetValue(context.ValueType, out var supplier))
        {
            supplier = (FormValueSupplier)Activator.CreateInstance(
                typeof(FormValueSupplier<>).MakeGenericType(context.ValueType))!;
            _suppliers.Add(context.ValueType, supplier);
        }

        supplier.Deserialize(context, options, formData);
    }

    private static bool MatchesScope(string handlerName, string scopeName, out ReadOnlySpan<char> formName)
    {
        if (handlerName.StartsWith('['))
        {
            var handlerSpan = handlerName.AsSpan();
            if (handlerSpan[1..].StartsWith(scopeName, StringComparison.Ordinal)
                && handlerName.Length >= scopeName.Length + 2
                && handlerName[scopeName.Length + 1] == ']')
            {
                formName = handlerSpan[(scopeName.Length + 2)..];
                return true;
            }
        }
        else if (string.IsNullOrEmpty(scopeName))
        {
            formName = handlerName;
            return true;
        }

        formName = default;
        return false;
    }

    private abstract class FormValueSupplier
    {
        [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
        public abstract void Deserialize(FormValueMappingContext context, FormDataMapperOptions options, McpFormDataProvider formData);
    }

    private sealed class FormValueSupplier<T> : FormValueSupplier
    {
        [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
        public override void Deserialize(FormValueMappingContext context, FormDataMapperOptions options, McpFormDataProvider formData)
        {
            if (formData.Entries.Count == 0)
            {
                return;
            }

            var dictionary = new Dictionary<FormKey, Microsoft.Extensions.Primitives.StringValues>();
            foreach (var (key, value) in formData.Entries)
            {
                dictionary.Add(new FormKey(key.AsMemory()), value);
            }

            var buffer = ArrayPool<char>.Shared.Rent(options.MaxKeyBufferSize);
            try
            {
                using var reader = new FormDataReader(
                    dictionary, CultureInfo.InvariantCulture, buffer.AsMemory(0, options.MaxKeyBufferSize), formData.Files)
                {
                    ErrorHandler = context.OnError,
                    AttachInstanceToErrorsHandler = context.MapErrorToContainer,
                    MaxRecursionDepth = options.MaxRecursionDepth,
                    MaxErrorCount = options.MaxErrorCount,
                    MaxCollectionSize = options.MaxCollectionSize
                };

                reader.PushPrefix(context.ParameterName);
                context.SetResult(FormDataMapper.Map<T>(reader, options));
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }
    }
}
