// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Example.Hello;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Grpc.Shared;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests.Infrastructure;
using Transcoding;
using Xunit.Abstractions;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests.ConverterTests;

public class JsonConverterWriteTests
{
    private readonly ITestOutputHelper _output;

    public JsonConverterWriteTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CustomizedName()
    {
        var helloRequest = new HelloRequest
        {
            FieldName = "A field name"
        };

        AssertWrittenJson(helloRequest,
            new GrpcJsonSettings { IgnoreDefaultValues = true });
    }

    [Fact]
    public void NonAsciiString()
    {
        var helloRequest = new HelloRequest
        {
            Name = "This is a test 激光這兩個字是甚麼意思 string"
        };

        AssertWrittenJson(helloRequest, compareRawStrings: true);
    }

    [Fact]
    public void RepeatedStrings()
    {
        var helloRequest = new HelloRequest
        {
            Name = "test",
            RepeatedStrings =
            {
                "One",
                "Two",
                "Three"
            }
        };

        AssertWrittenJson(helloRequest);
    }

    [Fact]
    public void RepeatedDoubleValues()
    {
        var helloRequest = new HelloRequest
        {
            RepeatedDoubleValues =
            {
                1,
                1.1
            }
        };

        AssertWrittenJson(helloRequest);
    }

    [Fact]
    public void MapStrings()
    {
        var helloRequest = new HelloRequest
        {
            MapStrings =
            {
                ["name1"] = "value1",
                ["name2"] = "value2"
            }
        };

        AssertWrittenJson(helloRequest);
    }

    [Fact]
    public void MapKeyBool()
    {
        var helloRequest = new HelloRequest
        {
            MapKeybool =
            {
                [true] = "value1",
                [false] = "value2"
            }
        };

        AssertWrittenJson(helloRequest);
    }

    [Fact]
    public void MapKeyInt()
    {
        var helloRequest = new HelloRequest
        {
            MapKeyint =
            {
                [-1] = "value1",
                [0] = "value2",
                [0] = "value3"
            }
        };

        AssertWrittenJson(helloRequest);
    }

    [Fact]
    public void MapMessages()
    {
        var helloRequest = new HelloRequest
        {
            MapMessage =
            {
                ["name1"] = new HelloRequest.Types.SubMessage { Subfield = "value1" },
                ["name2"] = new HelloRequest.Types.SubMessage { Subfield = "value2" }
            }
        };

        AssertWrittenJson(helloRequest);
    }

    [Fact]
    public void DataTypes_DefaultValues()
    {
        var wrappers = new HelloRequest.Types.DataTypes();

        AssertWrittenJson(
            wrappers,
            new GrpcJsonSettings { WriteInt64sAsStrings = true });
    }

    [Fact]
    public void NullableWrappers_NaN()
    {
        var wrappers = new HelloRequest.Types.Wrappers
        {
            DoubleValue = double.NaN
        };

        AssertWrittenJson(wrappers);
    }

    [Fact]
    public void NullValue_Default()
    {
        var m = new NullValueContainer();

        AssertWrittenJson(m);
    }

    [Fact]
    public void NullValue_NonDefaultValue()
    {
        var m = new NullValueContainer
        {
            NullValue = (NullValue)1
        };

        AssertWrittenJson(m);
    }

    [Fact]
    public void NullableWrappers()
    {
        var wrappers = new HelloRequest.Types.Wrappers
        {
            BoolValue = true,
            BytesValue = ByteString.CopyFrom(Encoding.UTF8.GetBytes("Hello world")),
            DoubleValue = 1.1,
            FloatValue = 1.2f,
            Int32Value = 1,
            Int64Value = 2L,
            StringValue = "A string",
            Uint32Value = 3U,
            Uint64Value = 4UL
        };

        AssertWrittenJson(wrappers);
    }

    [Fact]
    public void NullableWrapper_Root_Int32()
    {
        var v = new Int32Value { Value = 1 };

        AssertWrittenJson(v);
    }

    [Fact]
    public void NullableWrapper_Root_Int64()
    {
        var v = new Int64Value { Value = 1 };

        AssertWrittenJson(v);
    }

    [Theory]
    [InlineData(true, @"""1""")]
    [InlineData(false, @"1")]
    public void NullableWrapper_Root_Int64_WriteAsStrings(bool writeInt64sAsStrings, string expectedJson)
    {
        var v = new Int64Value { Value = 1 };

        var descriptorRegistry = CreateDescriptorRegistry(typeof(Int64Value));
        var settings = new GrpcJsonSettings { WriteInt64sAsStrings = writeInt64sAsStrings };
        var jsonSerializerOptions = CreateSerializerOptions(settings, TypeRegistry.Empty, descriptorRegistry);
        var json = JsonSerializer.Serialize(v, jsonSerializerOptions);

        Assert.Equal(expectedJson, json);
    }

    [Theory]
    [InlineData(true, @"""2""")]
    [InlineData(false, @"2")]
    public void NullableWrapper_Root_UInt64_WriteAsStrings(bool writeInt64sAsStrings, string expectedJson)
    {
        var v = new UInt64Value { Value = 2 };

        var descriptorRegistry = CreateDescriptorRegistry(typeof(UInt64Value));
        var settings = new GrpcJsonSettings { WriteInt64sAsStrings = writeInt64sAsStrings };
        var jsonSerializerOptions = CreateSerializerOptions(settings, TypeRegistry.Empty, descriptorRegistry);
        var json = JsonSerializer.Serialize(v, jsonSerializerOptions);

        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void Any()
    {
        var helloRequest = new HelloRequest
        {
            Name = "In any!"
        };
        var any = Google.Protobuf.WellKnownTypes.Any.Pack(helloRequest);

        AssertWrittenJson(any);
    }

    [Fact]
    public void Any_WellKnownType_Timestamp()
    {
        var timestamp = Timestamp.FromDateTimeOffset(DateTimeOffset.UnixEpoch);
        var any = Google.Protobuf.WellKnownTypes.Any.Pack(timestamp);

        AssertWrittenJson(any);
    }

    [Fact]
    public void Any_WellKnownType_Int32()
    {
        var value = new Int32Value() { Value = int.MaxValue };
        var any = Google.Protobuf.WellKnownTypes.Any.Pack(value);

        AssertWrittenJson(any);
    }

    [Fact]
    public void Timestamp_Nested()
    {
        var helloRequest = new HelloRequest
        {
            TimestampValue = Timestamp.FromDateTimeOffset(new DateTimeOffset(2020, 12, 1, 12, 30, 0, TimeSpan.FromHours(12)))
        };

        AssertWrittenJson(helloRequest);
    }

    [Fact]
    public void Timestamp_Root()
    {
        var ts = Timestamp.FromDateTimeOffset(new DateTimeOffset(2020, 12, 1, 12, 30, 0, TimeSpan.FromHours(12)));

        AssertWrittenJson(ts);
    }

    [Fact]
    public void Duration_Nested()
    {
        var helloRequest = new HelloRequest
        {
            DurationValue = Duration.FromTimeSpan(TimeSpan.FromHours(12))
        };

        AssertWrittenJson(helloRequest);
    }

    [Fact]
    public void Duration_Root()
    {
        var duration = Duration.FromTimeSpan(TimeSpan.FromHours(12));

        AssertWrittenJson(duration);
    }

    [Fact]
    public void Value_Nested()
    {
        var helloRequest = new HelloRequest
        {
            ValueValue = Value.ForStruct(new Struct
            {
                Fields =
                {
                    ["enabled"] = Value.ForBool(true),
                    ["metadata"] = Value.ForList(
                        Value.ForString("value1"),
                        Value.ForString("value2"))
                }
            })
        };

        AssertWrittenJson(helloRequest);
    }

    [Fact]
    public void Struct_NullValue()
    {
        var helloRequest = new HelloRequest
        {
            ValueValue = Value.ForStruct(new Struct
            {
                Fields =
                {
                    ["prop"] = Value.ForNull()
                }
            })
        };

        AssertWrittenJson(helloRequest);
    }

    [Fact]
    public void Value_Root()
    {
        var value = Value.ForStruct(new Struct
        {
            Fields =
            {
                ["enabled"] = Value.ForBool(true),
                ["metadata"] = Value.ForList(
                    Value.ForString("value1"),
                    Value.ForString("value2"))
            }
        });

        AssertWrittenJson(value);
    }

    [Fact]
    public void Value_Null()
    {
        var value = Value.ForNull();

        AssertWrittenJson(value);
    }

    [Fact]
    public void Struct_Nested()
    {
        var helloRequest = new HelloRequest
        {
            StructValue = new Struct
            {
                Fields =
                {
                    ["enabled"] = Value.ForBool(true),
                    ["metadata"] = Value.ForList(
                        Value.ForString("value1"),
                        Value.ForString("value2"))
                }
            }
        };

        AssertWrittenJson(helloRequest);
    }

    [Fact]
    public void Struct_Root()
    {
        var value = new Struct
        {
            Fields =
            {
                ["enabled"] = Value.ForBool(true),
                ["metadata"] = Value.ForList(
                    Value.ForString("value1"),
                    Value.ForString("value2"))
            }
        };

        AssertWrittenJson(value);
    }

    [Fact]
    public void ListValue_Nested()
    {
        var helloRequest = new HelloRequest
        {
            ListValue = new ListValue
            {
                Values =
                {
                    Value.ForBool(true),
                    Value.ForString("value1"),
                    Value.ForString("value2")
                }
            }
        };

        AssertWrittenJson(helloRequest);
    }

    [Fact]
    public void ListValue_Root()
    {
        var value = new ListValue
        {
            Values =
            {
                Value.ForBool(true),
                Value.ForString("value1"),
                Value.ForString("value2")
            }
        };

        AssertWrittenJson(value);
    }

    [Fact]
    public void FieldMask_Nested()
    {
        var helloRequest = new HelloRequest
        {
            FieldMaskValue = FieldMask.FromString("value1,value2,value3.nested_value"),
        };

        AssertWrittenJson(helloRequest);
    }

    [Fact]
    public void FieldMask_Root()
    {
        var m = FieldMask.FromString("value1,value2,value3.nested_value");

        AssertWrittenJson(m);
    }

    [Theory]
    [InlineData(HelloRequest.Types.DataTypes.Types.NestedEnum.Unspecified)]
    [InlineData(HelloRequest.Types.DataTypes.Types.NestedEnum.Bar)]
    [InlineData(HelloRequest.Types.DataTypes.Types.NestedEnum.Neg)]
    [InlineData((HelloRequest.Types.DataTypes.Types.NestedEnum)100)]
    public void Enum(HelloRequest.Types.DataTypes.Types.NestedEnum value)
    {
        var dataTypes = new HelloRequest.Types.DataTypes
        {
            SingleEnum = value
        };

        AssertWrittenJson(dataTypes);
    }

    [Theory]
    [InlineData(HelloRequest.Types.DataTypes.Types.NestedEnum.Unspecified)]
    [InlineData(HelloRequest.Types.DataTypes.Types.NestedEnum.Bar)]
    [InlineData(HelloRequest.Types.DataTypes.Types.NestedEnum.Neg)]
    [InlineData((HelloRequest.Types.DataTypes.Types.NestedEnum)100)]
    public void Enum_WriteNumber(HelloRequest.Types.DataTypes.Types.NestedEnum value)
    {
        var dataTypes = new HelloRequest.Types.DataTypes
        {
            SingleEnum = value
        };

        AssertWrittenJson(dataTypes, new GrpcJsonSettings { WriteEnumsAsIntegers = true, IgnoreDefaultValues = true });
    }

    [Fact]
    public void Enum_Imported()
    {
        var m = new SayRequest();
        m.Country = Example.Country.Alpha3CountryCode.Afg;

        AssertWrittenJson(m);
    }

    // See See https://github.com/protocolbuffers/protobuf/issues/11987
    [Fact]
    public void JsonNamePriority()
    {
        var m = new Issue047349Message { A = 10, B = 20, C = 30 };
        var json = AssertWrittenJson(m);

        Assert.Equal(@"{""b"":10,""a"":20,""d"":30}", json);
    }

    private string AssertWrittenJson<TValue>(TValue value, GrpcJsonSettings? settings = null, bool? compareRawStrings = null) where TValue : IMessage
    {
        var typeRegistery = TypeRegistry.FromFiles(
            HelloRequest.Descriptor.File,
            Timestamp.Descriptor.File);

        settings ??= new GrpcJsonSettings { WriteInt64sAsStrings = true };

        var formatterSettings = new JsonFormatter.Settings(
            formatDefaultValues: !settings.IgnoreDefaultValues,
            typeRegistery);
        formatterSettings = formatterSettings.WithFormatEnumsAsIntegers(settings.WriteEnumsAsIntegers);
        var formatter = new JsonFormatter(formatterSettings);

        var jsonOld = formatter.Format(value);

        _output.WriteLine("Old:");
        _output.WriteLine(jsonOld);

        var descriptorRegistry = CreateDescriptorRegistry(typeof(TValue));
        var jsonSerializerOptions = CreateSerializerOptions(settings, typeRegistery, descriptorRegistry);
        var jsonNew = JsonSerializer.Serialize(value, jsonSerializerOptions);

        _output.WriteLine("New:");
        _output.WriteLine(jsonNew);

        using var doc1 = JsonDocument.Parse(jsonNew);
        using var doc2 = JsonDocument.Parse(jsonOld);

        var comparer = new JsonElementComparer(maxHashDepth: -1, compareRawStrings: compareRawStrings ?? false);
        Assert.True(comparer.Equals(doc1.RootElement, doc2.RootElement));

        return jsonNew;
    }

    private static DescriptorRegistry CreateDescriptorRegistry(Type type)
    {
        var descriptorRegistry = new DescriptorRegistry();
        descriptorRegistry.RegisterFileDescriptor(TestHelpers.GetMessageDescriptor(type).File);
        return descriptorRegistry;
    }

    internal static JsonSerializerOptions CreateSerializerOptions(GrpcJsonSettings? settings, TypeRegistry? typeRegistery, DescriptorRegistry descriptorRegistry)
    {
        var context = new JsonContext(settings ?? new GrpcJsonSettings(), typeRegistery ?? TypeRegistry.Empty, descriptorRegistry);

        return JsonConverterHelper.CreateSerializerOptions(context);
    }
}
