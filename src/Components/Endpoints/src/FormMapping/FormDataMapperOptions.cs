// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class FormDataMapperOptions
{
    private readonly ConcurrentDictionary<Type, FormDataConverter> _converters = new();
    private readonly List<IFormDataConverterFactory> _factories = new();

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public FormDataMapperOptions() : this(NullLoggerFactory.Instance)
    {        
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public FormDataMapperOptions(ILoggerFactory loggerFactory)
    {
        _converters = new(WellKnownConverters.Converters);
        _factories.Add(new ParsableConverterFactory());
        _factories.Add(new EnumConverterFactory());
        _factories.Add(new NullableConverterFactory());
        _factories.Add(new DictionaryConverterFactory());
        _factories.Add(new CollectionConverterFactory());
        _factories.Add(new ComplexTypeConverterFactory(this, loggerFactory));
        Logger = loggerFactory.CreateLogger<FormDataMapperOptions>();
    }

    internal ILogger Logger { get; }

    // Not configurable for now, this is the max number of elements we will bind. This is important for
    // security reasons, as we don't want to bind a huge collection and cause perf issues.
    // Some examples of this are:
    // Binding to collection using hashes, where the payload can be crafted to force the worst case on insertion
    // which is O(n).
    internal int MaxCollectionSize = FormReader.DefaultValueCountLimit;

    // MVC uses 32, JSON uses 64. Let's stick to STJ default.
    internal int MaxRecursionDepth = 64;

    // This is normally 200 (similar to ModelStateDictionary.DefaultMaxAllowedErrors in MVC) 
    internal int MaxErrorCount = 200;

    internal int MaxKeyBufferSize = FormReader.DefaultKeyLengthLimit;

    internal bool UseCurrentCulture;

    // For testing purposes only.
    internal List<IFormDataConverterFactory> Factories => _factories;

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    internal FormDataConverter<T> ResolveConverter<T>()
    {
        return (FormDataConverter<T>)_converters.GetOrAdd(typeof(T), CreateConverter, this);
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    private static FormDataConverter CreateConverter(Type type, FormDataMapperOptions options)
    {
        foreach (var factory in options._factories)
        {
            if (factory.CanConvert(type, options))
            {
                return factory.CreateConverter(type, options);
            }
        }

        throw new InvalidOperationException($"No converter registered for type '{type.FullName}'.");
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    internal FormDataConverter ResolveConverter(Type type)
    {
        return _converters.GetOrAdd(type, CreateConverter, this);
    }

    // For testing
    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    internal void AddConverter<T>(FormDataConverter<T> converter)
    {
        _converters[typeof(T)] = converter;
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    internal bool CanConvert(Type type)
    {
        if (_converters.ContainsKey(type))
        {
            return true;
        }

        foreach (var factory in _factories)
        {
            if (factory.CanConvert(type, this))
            {
                return true;
            }
        }

        return false;
    }
}
