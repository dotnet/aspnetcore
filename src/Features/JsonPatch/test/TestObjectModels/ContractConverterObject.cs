// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch;

// Has no public members, so Newtonsoft.Json's defaults write it as {}.
// Its JSON form, a string, comes only from the converter that
// CodeContractResolver assigns to its contract.
public readonly struct Code
{
    private readonly string _value;

    public Code(string value)
    {
        _value = value;
    }

    public override string ToString() => _value;
}

public class ContractConverterObject
{
    public Code Code { get; set; }

    public Code BackupCode { get; set; }
}

public class CodeContractResolver : DefaultContractResolver
{
    protected override JsonConverter ResolveContractConverter(Type objectType)
    {
        if (objectType == typeof(Code))
        {
            return new CodeJsonConverter();
        }

        return base.ResolveContractConverter(objectType);
    }
}

public class CodeJsonConverter : JsonConverter<Code>
{
    public override Code ReadJson(JsonReader reader, Type objectType, Code existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return new Code((string)reader.Value);
    }

    public override void WriteJson(JsonWriter writer, Code value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }
}
