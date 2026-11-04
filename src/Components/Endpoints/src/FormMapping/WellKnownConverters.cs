// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if COMPONENTS
using Microsoft.AspNetCore.Components.Forms;
#endif
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal static class WellKnownConverters
{
    public static readonly IReadOnlyDictionary<Type, FormDataConverter> Converters;

#pragma warning disable CA1810 // Initialize reference type static fields inline
    static WellKnownConverters()
#pragma warning restore CA1810 // Initialize reference type static fields inline
    {
        var converters = new Dictionary<Type, FormDataConverter>
        {
            // For the most common types, we avoid going through the factories and just
            // create the converters directly. This is a performance optimization.
            { typeof(string), new ParsableConverter<string>() },
            { typeof(char), new ParsableConverter<char>() },
            { typeof(bool), new ParsableConverter<bool>() },
            { typeof(byte), new ParsableConverter<byte>() },
            { typeof(sbyte), new ParsableConverter<sbyte>() },
            { typeof(ushort), new ParsableConverter<ushort>() },
            { typeof(uint), new ParsableConverter<uint>() },
            { typeof(ulong), new ParsableConverter<ulong>() },
            { typeof(Int128), new ParsableConverter<Int128>() },
            { typeof(short), new ParsableConverter<short>() },
            { typeof(int), new ParsableConverter<int>() },
            { typeof(long), new ParsableConverter<long>() },
            { typeof(UInt128), new ParsableConverter<UInt128>() },
            { typeof(Half), new ParsableConverter<Half>() },
            { typeof(float), new ParsableConverter<float>() },
            { typeof(double), new ParsableConverter<double>() },
            { typeof(decimal), new ParsableConverter<decimal>() },
            { typeof(DateOnly), new ParsableConverter<DateOnly>() },
            { typeof(DateTime), new ParsableConverter<DateTime>() },
            { typeof(DateTimeOffset), new ParsableConverter<DateTimeOffset>() },
            { typeof(TimeSpan), new ParsableConverter<TimeSpan>() },
            { typeof(TimeOnly), new ParsableConverter<TimeOnly>() },
            { typeof(Guid), new ParsableConverter<Guid>() },
            { typeof(IFormFileCollection), new FileConverter<IFormFileCollection>() },
            { typeof(IFormFile), new FileConverter<IFormFile>() },
            { typeof(IReadOnlyList<IFormFile>), new FileConverter<IReadOnlyList<IFormFile>>() },
            { typeof(Uri), new UriFormDataConverter() },
#if COMPONENTS
            { typeof(IBrowserFile), new FileConverter<IBrowserFile>() },
            { typeof(IReadOnlyList<IBrowserFile>), new FileConverter<IReadOnlyList<IBrowserFile>>() }
#endif
        };

        converters.Add(typeof(char?), new NullableConverter<char>((FormDataConverter<char>)converters[typeof(char)]));
        converters.Add(typeof(bool?), new NullableConverter<bool>((FormDataConverter<bool>)converters[typeof(bool)]));
        converters.Add(typeof(byte?), new NullableConverter<byte>((FormDataConverter<byte>)converters[typeof(byte)]));
        converters.Add(typeof(sbyte?), new NullableConverter<sbyte>((FormDataConverter<sbyte>)converters[typeof(sbyte)]));
        converters.Add(typeof(ushort?), new NullableConverter<ushort>((FormDataConverter<ushort>)converters[typeof(ushort)]));
        converters.Add(typeof(uint?), new NullableConverter<uint>((FormDataConverter<uint>)converters[typeof(uint)]));
        converters.Add(typeof(ulong?), new NullableConverter<ulong>((FormDataConverter<ulong>)converters[typeof(ulong)]));
        converters.Add(typeof(Int128?), new NullableConverter<Int128>((FormDataConverter<Int128>)converters[typeof(Int128)]));
        converters.Add(typeof(short?), new NullableConverter<short>((FormDataConverter<short>)converters[typeof(short)]));
        converters.Add(typeof(int?), new NullableConverter<int>((FormDataConverter<int>)converters[typeof(int)]));
        converters.Add(typeof(long?), new NullableConverter<long>((FormDataConverter<long>)converters[typeof(long)]));
        converters.Add(typeof(UInt128?), new NullableConverter<UInt128>((FormDataConverter<UInt128>)converters[typeof(UInt128)]));
        converters.Add(typeof(Half?), new NullableConverter<Half>((FormDataConverter<Half>)converters[typeof(Half)]));
        converters.Add(typeof(float?), new NullableConverter<float>((FormDataConverter<float>)converters[typeof(float)]));
        converters.Add(typeof(double?), new NullableConverter<double>((FormDataConverter<double>)converters[typeof(double)]));
        converters.Add(typeof(decimal?), new NullableConverter<decimal>((FormDataConverter<decimal>)converters[typeof(decimal)]));
        converters.Add(typeof(DateOnly?), new NullableConverter<DateOnly>((FormDataConverter<DateOnly>)converters[typeof(DateOnly)]));
        converters.Add(typeof(DateTime?), new NullableConverter<DateTime>((FormDataConverter<DateTime>)converters[typeof(DateTime)]));
        converters.Add(typeof(DateTimeOffset?), new NullableConverter<DateTimeOffset>((FormDataConverter<DateTimeOffset>)converters[typeof(DateTimeOffset)]));
        converters.Add(typeof(TimeSpan?), new NullableConverter<TimeSpan>((FormDataConverter<TimeSpan>)converters[typeof(TimeSpan)]));
        converters.Add(typeof(TimeOnly?), new NullableConverter<TimeOnly>((FormDataConverter<TimeOnly>)converters[typeof(TimeOnly)]));
        converters.Add(typeof(Guid?), new NullableConverter<Guid>((FormDataConverter<Guid>)converters[typeof(Guid)]));

        Converters = converters;
    }
}
