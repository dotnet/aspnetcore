// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping.Metadata;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

// This factory is registered last, which means, dictionaries and collections, have already
// been processed by the time we get here.
internal class ComplexTypeConverterFactory(FormDataMapperOptions options, ILoggerFactory loggerFactory) : IFormDataConverterFactory
{
    internal FormDataMetadataFactory MetadataFactory { get; } = new FormDataMetadataFactory(options.Factories, loggerFactory);

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public bool CanConvert(Type type, FormDataMapperOptions options)
    {
        // Create the metadata for the type. This walks the graph and creates metadata for all the types
        // in the reference graph, detecting and identifying recursive types.
        var metadata = MetadataFactory.GetOrCreateMetadataFor(type, options);

        // If we can create metadata for the type, then we can convert it.
        return metadata != null;
    }

    // We are going to compile a function that maps all the properties for the type.
    // Beware that the code below is not the actual exact code, just a simplification to understand what is happening at a high level.
    // The general flow is as follows. For a type like Address { Street, City, CountryRegion, ZipCode }
    // we will generate a function that looks like:
    // public bool TryRead(ref FormDataReader reader, Type type, FormDataSerializerOptions options, out Address? result, out bool found)
    // {
    //     bool foundProperty;
    //     bool succeeded = true;
    //     string street;
    //     string city;
    //     string countryRegion;
    //     string zipCode;
    //     FormDataConveter<string> streetConverter;
    //     FormDataConveter<string> cityConverter;
    //     FormDataConveter<string> countryRegionConverter;
    //     FormDataConveter<string> zipCodeConverter;

    //     var streetConverter = options.ResolveConverter(typeof(string));
    //     reader.PushPrefix("Street");
    //     succeeded &= streetConverter.TryRead(ref reader, typeof(string), options, out street, out foundProperty);
    //     found ||= foundProperty;
    //     reader.PopPrefix("Street");
    //
    //     var cityConverter = options.ResolveConverter(typeof(string));
    //     reader.PushPrefix("City");
    //     succeeded &= ciryConverter.TryRead(ref reader, typeof(string), options, out street, out foundProperty);
    //     found ||= foundProperty;
    //     reader.PopPrefix("City");
    //
    //     var countryRegionConverter = options.ResolveConverter(typeof(string));
    //     reader.PushPrefix("CountryRegion");
    //     succeeded &= countryRegionConverter.TryRead(ref reader, typeof(string), options, out street, out foundProperty);
    //     found ||= foundProperty;
    //     reader.PopPrefix("CountryRegion");
    //
    //     var zipCodeConverter = options.ResolveConverter(typeof(string));
    //     reader.PushPrefix("ZipCode");
    //     succeeded &= zipCodeConverter.TryRead(ref reader, typeof(string), options, out street, out foundProperty);
    //     found ||= foundProperty;
    //     reader.PopPrefix("ZipCode");
    //
    //     if(found)
    //     {
    //         result = new Address();
    //         result.Street = street;
    //         result.City = city;
    //         result.CountryRegion = countryRegion;
    //         result.ZipCode = zipCode;
    //     }
    //     else
    //     {
    //         result = null;
    //     }
    //
    //     return succeeded;
    // }
    //
    // The actual blocks above are going to be generated using System.Linq.Expressions.
    // Instead of resolving the property converters every time, we might consider caching the converters in a dictionary and passing an
    // extra parameter to the function with them in it.
    // The final converter is something like
    // internal class CompiledComplexTypeConverter
    //     (ConverterDelegate<FormDataReader, Type, FormDataSerializerOptions, out object, out bool> converterFunc)
    // {
    //     public bool TryRead(ref FormDataReader reader, Type type, FormDataSerializerOptions options, out object? result, out bool found)
    //     {
    //         return converterFunc(ref reader, type, options, out result, out found);
    //     }
    // }
    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public FormDataConverter CreateConverter(Type type, FormDataMapperOptions options)
    {
        if (Activator.CreateInstance(typeof(ComplexTypeExpressionConverterFactory<>).MakeGenericType(type), MetadataFactory)
            is not ComplexTypeExpressionConverterFactory factory)
        {
            throw new InvalidOperationException($"Could not create a converter factory for type {type}.");
        }

        return factory.CreateConverter(type, options);
    }
}
