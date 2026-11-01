// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Adapters;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;

public class Operation : OperationBase
{
    internal struct ValueHolder : IEquatable<ValueHolder>
    {
        // While ShouldSerializeValue appears unread. Be careful if you remove it.
        // It decides whether or not the value property is serialized.
        // It's used by STJ for deciding the JsonIgnore (WhenWritingDefault)
        public bool ShouldSerializeValue { get; set; }

        public object Value { get; set; }

        public ValueHolder(bool shouldSerializeValue, object value)
        {
            ShouldSerializeValue = shouldSerializeValue;
            Value = value;
        }

        public bool Equals(ValueHolder other)
            => ShouldSerializeValue == other.ShouldSerializeValue && Equals(Value, other.Value);

        public override bool Equals([NotNullWhen(true)] object obj)
            => obj is ValueHolder other && Equals(other);

        public override int GetHashCode()
            => ShouldSerializeValue ? Value?.GetHashCode() ?? 0 : 0;
    }

    private sealed class ValueHolderConverter : JsonConverter<ValueHolder>
    {
        public override ValueHolder Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = JsonSerializer.Deserialize<object>(ref reader, options);
            return new ValueHolder(true, value);
        }

        public override void Write(Utf8JsonWriter writer, ValueHolder value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value.Value, options);
    }

    [JsonIgnore]
    public object value { get; set; }

    [JsonPropertyName(nameof(value))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(ValueHolderConverter))]
    [JsonInclude]
    internal ValueHolder Holder
    {
        get => OperationType is OperationType.Add or OperationType.Replace or OperationType.Test
              ? new ValueHolder(true, value)
              : default;

        set => this.value = value.Value;
    }

    public Operation()
    {
    }

    public Operation(string op, string path, string from, object value)
        : base(op, path, from)
    {
        this.value = value;
    }

    public Operation(string op, string path, string from)
        : base(op, path, from)
    {
    }

    public void Apply(object objectToApplyTo, IObjectAdapter adapter)
    {
        ArgumentNullThrowHelper.ThrowIfNull(objectToApplyTo);
        ArgumentNullThrowHelper.ThrowIfNull(adapter);

        switch (OperationType)
        {
            case OperationType.Add:
                adapter.Add(this, objectToApplyTo);
                break;
            case OperationType.Remove:
                adapter.Remove(this, objectToApplyTo);
                break;
            case OperationType.Replace:
                adapter.Replace(this, objectToApplyTo);
                break;
            case OperationType.Move:
                adapter.Move(this, objectToApplyTo);
                break;
            case OperationType.Copy:
                adapter.Copy(this, objectToApplyTo);
                break;
            case OperationType.Test:
                if (adapter is IObjectAdapterWithTest adapterWithTest)
                {
                    adapterWithTest.Test(this, objectToApplyTo);
                    break;
                }
                else
                {
                    throw new NotSupportedException(Resources.TestOperationNotSupported);
                }
            default:
                break;
        }
    }
}
