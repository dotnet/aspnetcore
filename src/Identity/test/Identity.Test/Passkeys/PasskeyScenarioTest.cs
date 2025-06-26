// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.AspNetCore.Identity.Test;

// Represents a test for a passkey scenario (attestation or assertion)
internal abstract class PasskeyScenarioTest<TResult>
{
    private bool _hasStarted;

    public Task<TResult> RunAsync()
    {
        if (_hasStarted)
        {
            throw new InvalidOperationException("The test can only be run once.");
        }

        _hasStarted = true;
        return RunCoreAsync();
    }

    protected abstract Task<TResult> RunCoreAsync();

    // While some test configuration can be set directly on scenario classes (AttestationTest and AssertionTest),
    // individual tests may need to modify values computed during execution (e.g., JSON payloads, hashes).
    // This helper enables trivial customization of test scenarios by allowing injection of custom logic to
    // transform runtime values.
    public class ComputedValue<TValue>
    {
        private bool _isComputed;
        private TValue? _computedValue;
        private Func<TValue, TValue?>? _transformFunc;

        public TValue GetValue()
        {
            if (!_isComputed)
            {
                throw new InvalidOperationException("Cannot get the value because it has not yet been computed.");
            }

            return _computedValue!;
        }

        public virtual TValue Compute(TValue initialValue)
        {
            if (_isComputed)
            {
                throw new InvalidOperationException("Cannot compute a value multiple times.");
            }

            if (_transformFunc is not null)
            {
                initialValue = _transformFunc(initialValue) ?? initialValue;
            }

            _isComputed = true;
            _computedValue = initialValue;
            return _computedValue;
        }

        public virtual void Transform(Func<TValue, TValue?> transform)
        {
            if (_transformFunc is not null)
            {
                throw new InvalidOperationException("Cannot transform a value multiple times.");
            }

            _transformFunc = transform;
        }
    }

    public sealed class ComputedJsonObject : ComputedValue<string>
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            WriteIndented = true,
        };

        private JsonElement? _jsonElementValue;

        public JsonElement GetValueAsJsonElement()
        {
            if (_jsonElementValue is null)
            {
                var rawValue = GetValue() ?? throw new InvalidOperationException("Cannot get the value as a JSON element because it is null.");
                try
                {
                    _jsonElementValue = JsonSerializer.Deserialize<JsonElement>(rawValue, _jsonSerializerOptions);
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException("Cannot get the value as a JSON element because it is not valid JSON.", ex);
                }
            }

            return _jsonElementValue.Value;
        }

        public void TransformAsJsonObject(Action<JsonObject> transform)
        {
            Transform(value =>
            {
                try
                {
                    var jsonObject = JsonNode.Parse(value)?.AsObject()
                        ?? throw new InvalidOperationException("Could not transform the JSON value because it was unexpectedly null.");
                    transform(jsonObject);
                    return jsonObject.ToJsonString(_jsonSerializerOptions);
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException("Could not transform the value because it was not valid JSON.", ex);
                }
            });
        }
    }
}
