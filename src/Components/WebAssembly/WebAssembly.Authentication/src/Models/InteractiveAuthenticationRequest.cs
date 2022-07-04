// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Represents the request to the identity provider for logging in or provisioning a token.
/// </summary>
public sealed class InteractiveAuthenticationRequest
{
    private Dictionary<string, object> _additionalRequestParameters;

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
    public string ReturnUrl { get; }

    /// <summary>
    /// Gets the scopes this request must use in the operation.
    /// </summary>
    public IEnumerable<string> Scopes { get; }

    /// <summary>
    /// Gets the request type.
    /// </summary>
    public InteractiveAuthenticationRequestType RequestType { get; }

    public void AddAdditionalParameter<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TParameter>(string name, TParameter value)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        _additionalRequestParameters ??= new();
        _additionalRequestParameters.Add(name, value);
    }

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
    internal string ToState() => JsonSerializer.Serialize(new InteractiveAuthenticationRequestRecord(RequestType, ReturnUrl, Scopes, _additionalRequestParameters));

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Deserializes InteractiveAuthenticationRequestRecord")]
    internal static InteractiveAuthenticationRequest FromState(string state) => new(JsonSerializer.Deserialize<InteractiveAuthenticationRequestRecord>(state));

    // We use a record to perform serialization due to limitations in System.Text.Json to serialize/deserialize non public properties.
    private record struct InteractiveAuthenticationRequestRecord(
        InteractiveAuthenticationRequestType RequestType, string ReturnUrl, IEnumerable<string> Scopes, Dictionary<string, object> AdditionalRequestParameters);
}
