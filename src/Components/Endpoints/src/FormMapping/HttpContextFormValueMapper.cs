// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class HttpContextFormValueMapper : IFormValueMapper
{
    private readonly HttpContextFormDataProvider _formData;
    private readonly FormDataMapperOptions _options;
    private static readonly ConcurrentDictionary<Type, FormValueSupplier> _cache = new();

    public HttpContextFormValueMapper(
        HttpContextFormDataProvider formData,
        IOptions<RazorComponentsServiceOptions> options)
    {
        _formData = formData;
        _options = options.Value._formMappingOptions;
    }

    public bool CanMap(Type valueType, string scopeName, string? formName)
    {
        // We must always match on scope
        if (!_formData.TryGetIncomingHandlerName(out var incomingScopeQualifiedFormName)
            || !MatchesScope(incomingScopeQualifiedFormName, scopeName, out var incomingFormName))
        {
            return false;
        }

        // Matching on formname is optional, enforced only if a nonempty form name was demanded by the receiver
        if (formName is not null && !incomingFormName.Equals(formName, StringComparison.Ordinal))
        {
            return false;
        }

        return _options.ResolveConverter(valueType) is not null;
    }

    private static bool MatchesScope(string incomingScopeQualifiedFormName, string currentMappingScopeName, out ReadOnlySpan<char> incomingFormName)
    {
        if (incomingScopeQualifiedFormName.StartsWith('['))
        {
            // The scope-qualified name is in the form "[scopename]formname", so validate that the [scopename]
            // prefix matches and return the formname part
            var incomingScopeQualifiedFormNameSpan = incomingScopeQualifiedFormName.AsSpan();
            if (incomingScopeQualifiedFormNameSpan[1..].StartsWith(currentMappingScopeName, StringComparison.Ordinal)
                && incomingScopeQualifiedFormName.Length >= currentMappingScopeName.Length + 2
                && incomingScopeQualifiedFormName[currentMappingScopeName.Length + 1] == ']')
            {
                incomingFormName = incomingScopeQualifiedFormNameSpan[(currentMappingScopeName.Length + 2)..];
                return true;
            }
        }
        else
        {
            // The scope-qualified name is in the form "formname", so validating that the scopename matches
            // means checking that it's empty
            if (string.IsNullOrEmpty(currentMappingScopeName))
            {
                incomingFormName = incomingScopeQualifiedFormName;
                return true;
            }
        }

        incomingFormName = default;
        return false;
    }

    public void Map(FormValueMappingContext context)
    {
        // This will func to a proper binder
        if (!CanMap(context.ValueType, context.AcceptMappingScopeName, context.AcceptFormName))
        {
            context.SetResult(null);
        }

        var deserializer = _cache.GetOrAdd(context.ValueType, CreateDeserializer);
        Debug.Assert(deserializer != null);
        deserializer.Deserialize(context, _options, _formData.Entries, _formData.FormFiles);
    }

    private FormValueSupplier CreateDeserializer(Type type) =>
        (FormValueSupplier)Activator.CreateInstance(typeof(FormValueSupplier<>)
        .MakeGenericType(type))!;

    internal abstract class FormValueSupplier
    {
        [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
        public abstract void Deserialize(
            FormValueMappingContext context,
            FormDataMapperOptions options,
            IReadOnlyDictionary<string, StringValues> form,
            IFormFileCollection formFiles);
    }

    internal class FormValueSupplier<T> : FormValueSupplier
    {
        [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
        public override void Deserialize(
            FormValueMappingContext context,
            FormDataMapperOptions options,
            IReadOnlyDictionary<string, StringValues> form,
            IFormFileCollection formFiles)
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

                using var reader = new FormDataReader(
                    dictionary,
                    CultureInfo.InvariantCulture,
                    buffer.AsMemory(0, options.MaxKeyBufferSize),
                    formFiles)
                {
                    ErrorHandler = context.OnError,
                    AttachInstanceToErrorsHandler = context.MapErrorToContainer,
                    MaxRecursionDepth = options.MaxRecursionDepth,
                    MaxErrorCount = options.MaxErrorCount
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
