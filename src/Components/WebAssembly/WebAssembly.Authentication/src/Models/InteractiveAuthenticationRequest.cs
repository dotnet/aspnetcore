// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Represents the request to the identity provider for logging in or provisioning a token.
/// </summary>
[JsonConverter(typeof(Converter))]
public sealed class InteractiveAuthenticationRequest
{
    private Dictionary<string, object> _additionalRequestParameters;

    /// <summary>
    /// Initializes a new instance of <see cref="InteractiveAuthenticationRequest"/>.
    /// </summary>
    /// <param name="request">The <see cref="InteractiveAuthenticationRequestType"/>.</param>
    /// <param name="returnUrl">The URL to return to after the interactive operation.</param>
    /// <param name="scopes">The scopes to request interactively.</param>
    public InteractiveAuthenticationRequest(InteractiveAuthenticationRequestType request, string returnUrl, IEnumerable<string> scopes)
    {
        RequestType = request;
        ReturnUrl = returnUrl;
        Scopes = scopes;
    }

    private InteractiveAuthenticationRequest(InteractiveAuthenticationRequestRecord record) =>
        (RequestType, ReturnUrl, Scopes, _additionalRequestParameters) = record;

    /// <summary>
    /// Gets the redirect URL this request must return to upon successful completion.
    /// </summary>
    public string ReturnUrl { get; private set; }

    /// <summary>
    /// Gets the scopes this request must use in the operation.
    /// </summary>
    public IEnumerable<string> Scopes { get; private set; }

    /// <summary>
    /// Gets the request type.
    /// </summary>
    public InteractiveAuthenticationRequestType RequestType { get; private set; }

    /// <summary>
    /// Adds an additional parameter to the interactive request.
    /// </summary>
    /// <remarks>
    /// The parameter will be received by the provider which can decide to honor or ignore it.
    /// </remarks>
    /// <typeparam name="TParameter">The parameter type.</typeparam>
    /// <param name="name">The parameter name.</param>
    /// <param name="value"> The parameter value.</param>
    public void AddAdditionalParameter<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TParameter>(string name, TParameter value)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        _additionalRequestParameters ??= new();
        _additionalRequestParameters.Add(name, value);
    }

    /// <summary>
    /// Retrieves an additional parameter previously added from the request.
    /// </summary>
    /// <typeparam name="TParameter">The expected parameter type.</typeparam>
    /// <param name="name">The parameter name.</param>
    /// <returns>The parameter value.</returns>
    /// <exception cref="InvalidOperationException">The parameter has a different type than the expected type.</exception>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Can potentially deserialize the parameter type TParameter")]
    public TParameter GetAdditionalParameter<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TParameter>(string name)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        if (_additionalRequestParameters == null || !_additionalRequestParameters.TryGetValue(name, out var parameter) || parameter == null)
        {
            return default;
        }

        if (parameter is TParameter deserialized)
        {
            return deserialized;
        }

        if (parameter is JsonElement json)
        {
            deserialized = json.Deserialize<TParameter>();
            _additionalRequestParameters[name] = deserialized;
            return deserialized;
        }

        throw new InvalidOperationException($"Expected parameter '{name}' to be of type '{typeof(TParameter).FullName}' but found value of type '{parameter.GetType().FullName}'.");
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Serializes 'InteractiveAuthenticationRequest' into a string")]
    internal string ToState() => JsonSerializer.Serialize(this);

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Deserializes InteractiveAuthenticationRequestRecord")]
    internal static InteractiveAuthenticationRequest FromState(string state) => JsonSerializer.Deserialize<InteractiveAuthenticationRequest>(state);

    internal class Converter : JsonConverter<InteractiveAuthenticationRequest>
    {
        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
            Justification = "Deserializes 'InteractiveAuthenticationRequest'")]
        public override InteractiveAuthenticationRequest Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert != typeof(InteractiveAuthenticationRequest))
            {
                throw new InvalidProgramException($"wrong type: {typeToConvert.FullName}");
            }

            return new(JsonSerializer.Deserialize<InteractiveAuthenticationRequestRecord>(ref reader, options));
        }

        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
            Justification = "Serializes 'InteractiveAuthenticationRequest'")]
        public override void Write(Utf8JsonWriter writer, InteractiveAuthenticationRequest value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(
                writer,
                new InteractiveAuthenticationRequestRecord(value.RequestType, value.ReturnUrl, value.Scopes, value._additionalRequestParameters),
                options);
        }
    }

    // We use a record to perform serialization due to limitations in System.Text.Json to serialize/deserialize non public properties.
    private record struct InteractiveAuthenticationRequestRecord(
        InteractiveAuthenticationRequestType RequestType, string ReturnUrl, IEnumerable<string> Scopes, Dictionary<string, object> AdditionalRequestParameters);
}
