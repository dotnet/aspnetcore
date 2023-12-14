// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests.ConverterTests;

public class JsonConverterReadTests
{
    private readonly ITestOutputHelper _output;

    public JsonConverterReadTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void NonJsonName()
    {
        var json = @"{
  ""hiding_field_name"": ""A field name""
}";

        var m = AssertReadJson<HelloRequest>(json);
        Assert.Equal("A field name", m.HidingFieldName);
    }

    [Fact]
    public void HidingJsonName()
    {
        var json = @"{
  ""field_name"": ""A field name""
}";

        var m = AssertReadJson<HelloRequest>(json);
        Assert.Equal("", m.FieldName);
        Assert.Equal("A field name", m.HidingFieldName);
    }

    [Fact]
    public void JsonCustomizedName()
    {
        var json = @"{
  ""json_customized_name"": ""A field name""
}";

        var m = AssertReadJson<HelloRequest>(json);
        Assert.Equal("A field name", m.FieldName);
    }

    [Fact]
    public void ReadObjectProperties()
    {
        var json = @"{
  ""name"": ""test"",
  ""age"": 1
}";

        AssertReadJson<HelloRequest>(json);
    }

    [Fact]
    public void ReadNullStringProperty()
    {
        var json = @"{
  ""name"": null
}";

        AssertReadJson<HelloRequest>(json);
    }

    [Fact]
    public void ReadNullIntProperty()
    {
        var json = @"{
  ""age"": null
}";

        AssertReadJson<HelloRequest>(json);
    }

    [Fact]
    public void ReadNullProperties()
    {
        var json = @"{
  ""age"": null,
  ""nullValue"": null,
  ""json_customized_name"": null,
  ""field_name"": null,
  ""oneof_name1"": null,
  ""sub"": null,
  ""timestamp_value"": null
}";

        AssertReadJson<HelloRequest>(json);
    }

    [Fact]
    public void RepeatedStrings()
    {
        var json = @"{
  ""name"": ""test"",
  ""repeatedStrings"": [
    ""One"",
    ""Two"",
    ""Three""
  ]
}";

        AssertReadJson<HelloRequest>(json);
    }

    [Fact]
    public void Struct_NullProperty()
    {
        var json = @"{ ""prop"": null }";

        AssertReadJson<Struct>(json);
    }

    [Fact]
    public void Value_Null()
    {
        var json = "null";

        AssertReadJson<Value>(json);
    }

    [Fact]
    public void Value_Integer()
    {
        var json = "1";

        AssertReadJson<Value>(json);
    }

    [Fact]
    public void Value_String()
    {
        var json = @"""string!""";

        AssertReadJson<Value>(json);
    }

    [Fact]
    public void Value_Boolean()
    {
        var json = "true";

        AssertReadJson<Value>(json);
    }

    [Fact]
    public void DataTypes_DefaultValues()
    {
        var json = @"{
  ""singleInt32"": 0,
  ""singleInt64"": ""0"",
  ""singleUint32"": 0,
  ""singleUint64"": ""0"",
  ""singleSint32"": 0,
  ""singleSint64"": ""0"",
  ""singleFixed32"": 0,
  ""singleFixed64"": ""0"",
  ""singleSfixed32"": 0,
  ""singleSfixed64"": ""0"",
  ""singleFloat"": 0,
  ""singleDouble"": 0,
  ""singleBool"": false,
  ""singleString"": """",
  ""singleBytes"": """",
  ""singleEnum"": ""NESTED_ENUM_UNSPECIFIED""
}";

        var serviceDescriptorRegistry = new DescriptorRegistry();
        serviceDescriptorRegistry.RegisterFileDescriptor(JsonTranscodingGreeter.Descriptor.File);

        AssertReadJson<HelloRequest.Types.DataTypes>(json, descriptorRegistry: serviceDescriptorRegistry);
    }

    [Fact]
    public void DataTypes_NullValues()
    {
        var json = @"{
  ""singleInt32"": null,
  ""singleInt64"": null,
  ""singleUint32"": null,
  ""singleUint64"": null,
  ""singleSint32"": null,
  ""singleSint64"": null,
  ""singleFixed32"": null,
  ""singleFixed64"": null,
  ""singleSfixed32"": null,
  ""singleSfixed64"": null,
  ""singleFloat"": null,
  ""singleDouble"": null,
  ""singleBool"": null,
  ""singleString"": null,
  ""singleBytes"": null,
  ""singleEnum"": null
}";

        var serviceDescriptorRegistry = new DescriptorRegistry();
        serviceDescriptorRegistry.RegisterFileDescriptor(JsonTranscodingGreeter.Descriptor.File);

        AssertReadJson<HelloRequest.Types.DataTypes>(json, descriptorRegistry: serviceDescriptorRegistry);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(100)]
    public void Enum_ReadNumber(int value)
    {
        var json = @"{ ""singleEnum"": " + value + " }";

        AssertReadJson<HelloRequest.Types.DataTypes>(json);
    }

    [Theory]
    [InlineData("FOO")]
    [InlineData("BAR")]
    [InlineData("NEG")]
    public void Enum_ReadString(string value)
    {
        var serviceDescriptorRegistry = new DescriptorRegistry();
        serviceDescriptorRegistry.RegisterFileDescriptor(JsonTranscodingGreeter.Descriptor.File);

        var json = @$"{{ ""singleEnum"": ""{value}"" }}";

        AssertReadJson<HelloRequest.Types.DataTypes>(json, descriptorRegistry: serviceDescriptorRegistry);
    }

    [Fact]
    public void Enum_ReadString_NotAllowedValue()
    {
        var serviceDescriptorRegistry = new DescriptorRegistry();
        serviceDescriptorRegistry.RegisterFileDescriptor(JsonTranscodingGreeter.Descriptor.File);

        var json = @"{ ""singleEnum"": ""INVALID"" }";

        AssertReadJsonError<HelloRequest.Types.DataTypes>(json, ex => Assert.Equal(@"Error converting value ""INVALID"" to enum type Transcoding.HelloRequest+Types+DataTypes+Types+NestedEnum.", ex.Message), descriptorRegistry: serviceDescriptorRegistry, deserializeOld: false);
    }

    [Fact]
    public void Timestamp_Nested()
    {
        var json = @"{ ""timestampValue"": ""2020-12-01T00:30:00Z"" }";

        AssertReadJson<HelloRequest>(json);
    }

    [Fact]
    public void Duration_Nested()
    {
        var json = @"{ ""durationValue"": ""43200s"" }";

        AssertReadJson<HelloRequest>(json);
    }

    [Fact]
    public void Value_Nested()
    {
        var json = @"{
  ""valueValue"": {
    ""enabled"": true,
    ""metadata"": [
      ""value1"",
      ""value2""
    ]
  }
}";

        AssertReadJson<HelloRequest>(json);
    }

    [Fact]
    public void Value_Root()
    {
        var json = @"{
  ""enabled"": true,
  ""metadata"": [
    ""value1"",
    ""value2""
  ]
}";

        AssertReadJson<Value>(json);
    }

    [Fact]
    public void Struct_Nested()
    {
        var json = @"{
  ""structValue"": {
    ""enabled"": true,
    ""metadata"": [
      ""value1"",
      ""value2""
    ]
  }
}";

        AssertReadJson<HelloRequest>(json);
    }

    [Fact]
    public void Struct_Root()
    {
        var json = @"{
  ""enabled"": true,
  ""metadata"": [
    ""value1"",
    ""value2""
  ]
}";

        AssertReadJson<Struct>(json);
    }

    [Fact]
    public void ListValue_Nested()
    {
        var json = @"{
  ""listValue"": [
    true,
    ""value1"",
    ""value2""
  ]
}";

        AssertReadJson<HelloRequest>(json);
    }

    [Fact]
    public void ListValue_Root()
    {
        var json = @"[
  true,
  ""value1"",
  ""value2""
]";

        AssertReadJson<ListValue>(json);
    }

    [Fact]
    public void Int64_ReadNumber()
    {
        var json = @"{
  ""singleInt64"": 1,
  ""singleUint64"": 2,
  ""singleSint64"": 3,
  ""singleFixed64"": 4,
  ""singleSfixed64"": 5
}";

        AssertReadJson<HelloRequest.Types.DataTypes>(json);
    }

    [Fact]
    public void RepeatedDoubleValues()
    {
        var json = @"{
  ""repeatedDoubleValues"": [
    1,
    1.1
  ]
}";

        AssertReadJson<HelloRequest>(json);
    }

    [Fact]
    public void Any()
    {
        var json = @"{
  ""@type"": ""type.googleapis.com/transcoding.HelloRequest"",
  ""name"": ""In any!""
}";

        var any = AssertReadJson<Any>(json);
        var helloRequest = any.Unpack<HelloRequest>();
        Assert.Equal("In any!", helloRequest.Name);
    }

    [Fact]
    public void Any_WellKnownType_Timestamp()
    {
        var json = @"{
  ""@type"": ""type.googleapis.com/google.protobuf.Timestamp"",
  ""value"": ""1970-01-01T00:00:00Z""
}";

        var any = AssertReadJson<Any>(json);
        var timestamp = any.Unpack<Timestamp>();
        Assert.Equal(DateTimeOffset.UnixEpoch, timestamp.ToDateTimeOffset());
    }

    [Fact]
    public void Any_WellKnownType_Int32()
    {
        var json = @"{
  ""@type"": ""type.googleapis.com/google.protobuf.Int32Value"",
  ""value"": 2147483647
}";

        var any = AssertReadJson<Any>(json);
        var value = any.Unpack<Int32Value>();
        Assert.Equal(2147483647, value.Value);
    }

    [Fact]
    public void MapMessages()
    {
        var json = @"{
  ""mapMessage"": {
    ""name1"": {
      ""subfield"": ""value1""
    },
    ""name2"": {
      ""subfield"": ""value2""
    }
  }
}";

        AssertReadJson<HelloRequest>(json);
    }

    [Fact]
    public void MapKeyBool()
    {
        var json = @"{
  ""mapKeybool"": {
    ""true"": ""value1"",
    ""false"": ""value2""
  }
}";

        AssertReadJson<HelloRequest>(json);
    }

    [Fact]
    public void MapKeyInt()
    {
        var json = @"{
  ""mapKeyint"": {
    ""-1"": ""value1"",
    ""0"": ""value3""
  }
}";

        AssertReadJson<HelloRequest>(json);
    }

    [Fact]
    public void OneOf_Success()
    {
        var json = @"{
  ""oneofName1"": ""test""
}";

        AssertReadJson<HelloRequest>(json);
    }

    [Fact]
    public void OneOf_Failure()
    {
        var json = @"{
  ""oneofName1"": ""test"",
  ""oneofName2"": ""test""
}";

        AssertReadJsonError<HelloRequest>(json, ex => Assert.Equal("Multiple values specified for oneof oneof_test", ex.Message.TrimEnd('.')));
    }

    [Fact]
    public void NullableWrappers_NaN()
    {
        var json = @"{
  ""doubleValue"": ""NaN""
}";

        AssertReadJson<HelloRequest.Types.Wrappers>(json);
    }

    [Fact]
    public void NullableWrappers_Null()
    {
        var json = @"{
  ""stringValue"": null,
  ""int32Value"": null,
  ""int64Value"": null,
  ""floatValue"": null,
  ""doubleValue"": null,
  ""boolValue"": null,
  ""uint32Value"": null,
  ""uint64Value"": null,
  ""bytesValue"": null
}";

        AssertReadJson<HelloRequest.Types.Wrappers>(json);
    }

    [Fact]
    public void NullableWrappers()
    {
        var json = @"{
  ""stringValue"": ""A string"",
  ""int32Value"": 1,
  ""int64Value"": ""2"",
  ""floatValue"": 1.2,
  ""doubleValue"": 1.1,
  ""boolValue"": true,
  ""uint32Value"": 3,
  ""uint64Value"": ""4"",
  ""bytesValue"": ""SGVsbG8gd29ybGQ=""
}";

        AssertReadJson<HelloRequest.Types.Wrappers>(json);
    }

    [Fact]
    public void NullValue_Default_Null()
    {
        var json = @"{ ""nullValue"": null }";

        AssertReadJson<NullValueContainer>(json);
    }

    [Fact]
    public void NullValue_Default_String()
    {
        var json = @"{ ""nullValue"": ""NULL_VALUE"" }";

        AssertReadJson<NullValueContainer>(json);
    }

    [Fact]
    public void NullValue_NonDefaultValue_Int()
    {
        var json = @"{ ""nullValue"": 1 }";

        AssertReadJson<NullValueContainer>(json);
    }

    [Fact]
    public void NullValue_NonDefaultValue_String()
    {
        var json = @"{ ""nullValue"": ""MONKEY"" }";

        AssertReadJsonError<NullValueContainer>(json, ex => Assert.Equal("Invalid enum value: MONKEY for enum type: google.protobuf.NullValue", ex.Message));
    }

    [Fact]
    public void FieldMask_Nested()
    {
        var json = @"{ ""fieldMaskValue"": ""value1,value2,value3.nestedValue"" }";

        AssertReadJson<HelloRequest>(json);
    }

    [Fact]
    public void FieldMask_Root()
    {
        var json = @"""value1,value2,value3.nestedValue""";

        AssertReadJson<FieldMask>(json);
    }

    [Fact]
    public void NullableWrapper_Root_Int32()
    {
        var json = @"1";

        AssertReadJson<Int32Value>(json);
    }

    [Fact]
    public void NullableWrapper_Root_Int64()
    {
        var json = @"""1""";

        AssertReadJson<Int64Value>(json);
    }

    [Fact]
    public void Enum_Imported()
    {
        var json = @"{""name"":"""",""country"":""ALPHA_3_COUNTRY_CODE_AFG""}";

        AssertReadJson<SayRequest>(json);
    }

    // See See https://github.com/protocolbuffers/protobuf/issues/11987
    [Fact]
    public void JsonNamePriority_JsonName()
    {
        var json = @"{""b"":10,""a"":20,""d"":30}";

        // TODO: Current Google.Protobuf version doesn't have fix. Update when available. 3.23.0 or later?
        var m = AssertReadJson<Issue047349Message>(json, serializeOld: false);

        Assert.Equal(10, m.A);
        Assert.Equal(20, m.B);
        Assert.Equal(30, m.C);
    }

    [Fact]
    public void JsonNamePriority_FieldNameFallback()
    {
        var json = @"{""b"":10,""a"":20,""c"":30}";

        // TODO: Current Google.Protobuf version doesn't have fix. Update when available. 3.23.0 or later?
        var m = AssertReadJson<Issue047349Message>(json, serializeOld: false);

        Assert.Equal(10, m.A);
        Assert.Equal(20, m.B);
        Assert.Equal(30, m.C);
    }

    private TValue AssertReadJson<TValue>(string value, GrpcJsonSettings? settings = null, DescriptorRegistry? descriptorRegistry = null, bool serializeOld = true) where TValue : IMessage, new()
    {
        var typeRegistery = TypeRegistry.FromFiles(
            HelloRequest.Descriptor.File,
            Timestamp.Descriptor.File);

        TValue? objectOld = default;

        if (serializeOld)
        {
            var formatter = new JsonParser(new JsonParser.Settings(
                recursionLimit: int.MaxValue,
                typeRegistery));

            objectOld = formatter.Parse<TValue>(value);
        }

        descriptorRegistry ??= new DescriptorRegistry();
        descriptorRegistry.RegisterFileDescriptor(TestHelpers.GetMessageDescriptor(typeof(TValue)).File);
        var jsonSerializerOptions = CreateSerializerOptions(settings, typeRegistery, descriptorRegistry);

        var objectNew = JsonSerializer.Deserialize<TValue>(value, jsonSerializerOptions)!;

        _output.WriteLine("New:");
        _output.WriteLine(objectNew.ToString());

        if (serializeOld)
        {
            Debug.Assert(objectOld != null);

            _output.WriteLine("Old:");
            _output.WriteLine(objectOld.ToString());

            Assert.True(objectNew.Equals(objectOld));
        }

        return objectNew;
    }

    private void AssertReadJsonError<TValue>(string value, Action<Exception> assertException, GrpcJsonSettings? settings = null, DescriptorRegistry? descriptorRegistry = null, bool deserializeOld = true) where TValue : IMessage, new()
    {
        var typeRegistery = TypeRegistry.FromFiles(
            HelloRequest.Descriptor.File,
            Timestamp.Descriptor.File);

        descriptorRegistry ??= new DescriptorRegistry();
        descriptorRegistry.RegisterFileDescriptor(TestHelpers.GetMessageDescriptor(typeof(TValue)).File);
        var jsonSerializerOptions = CreateSerializerOptions(settings, typeRegistery, descriptorRegistry);

        var ex = Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<TValue>(value, jsonSerializerOptions));
        assertException(ex);

        if (deserializeOld)
        {
            var formatter = new JsonParser(new JsonParser.Settings(
                recursionLimit: int.MaxValue,
                typeRegistery));

            ex = Assert.ThrowsAny<Exception>(() => formatter.Parse<TValue>(value));
            assertException(ex);
        }
    }

    internal static JsonSerializerOptions CreateSerializerOptions(GrpcJsonSettings? settings, TypeRegistry? typeRegistery, DescriptorRegistry descriptorRegistry)
    {
        var context = new JsonContext(
            settings ?? new GrpcJsonSettings(),
            typeRegistery ?? TypeRegistry.Empty,
            descriptorRegistry);

        return JsonConverterHelper.CreateSerializerOptions(context);
    }
}
