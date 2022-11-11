// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Represents the request to the identity provider for logging in or provisioning a token.
/// </summary>
[JsonConverter(typeof(Converter))]
public sealed class InteractiveRequestOptions
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        MaxDepth = 32,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Gets the request type.
    /// </summary>
    public required InteractionType Interaction { get; init; }

    /// <summary>
    /// Gets the redirect URL this request must return to upon successful completion.
    /// </summary>
    public required string ReturnUrl { get; init; }

    /// <summary>
    /// Gets the scopes this request must use in the operation.
    /// </summary>
    public IEnumerable<string> Scopes { get; init; }

    private Dictionary<string, object> AdditionalRequestParameters { get; set; }

    /// <summary>
    /// Tries to add an additional parameter to pass in to the underlying provider.
    /// </summary>
    /// <remarks>
    /// The underlying provider is free to apply these parameters as it sees fit or ignore them completely. In the default
    /// implementations the parameters will be JSON serialized using System.Text.Json and passed as a parameter to the
    /// underlying JavaScript implementation that handles the operation details.
    /// </remarks>
    public bool TryAddAdditionalParameter<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string name, TValue value)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        AdditionalRequestParameters ??= new();
        return AdditionalRequestParameters.TryAdd(name, value);
    }

    /// <summary>
    /// Tries to remove an existing additional parameter.
    /// </summary>
    public bool TryRemoveAdditionalParameter(string name)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        return AdditionalRequestParameters != null && AdditionalRequestParameters.Remove(name);
    }

    /// <summary>
    /// Tries to retrieve an existing additional parameter.
    /// </summary>
    public bool TryGetAdditionalParameter<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string name, out TValue value)
    {
        ArgumentNullException.ThrowIfNull(name);

        value = default;
        if (AdditionalRequestParameters == null || !AdditionalRequestParameters.TryGetValue(name, out var rawValue))
        {
            return false;
        }
        if (rawValue is JsonElement json)
        {
            value = Deserialize(json);
            AdditionalRequestParameters[name] = value;
            return true;
        }
        else
        {
            value = (TValue)rawValue;
            return true;
        }

        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
            Justification = "The types this method deserializes are anotated with 'DynamicallyAccessedMembers' to prevent them from being linked out as part of 'TryAddAdditionalParameter'.")]
        static TValue Deserialize(JsonElement element) => element.Deserialize<TValue>();
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "This method serializes InteractiveRequestOptions which has an 'AdditionalRequestParameters' that might contain user defined types but that have already been annotated by 'TryAddAdditionalParameter'.")]
    internal string ToState() => JsonSerializer.Serialize(this, SerializerOptions);

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "This method deserializes InteractiveRequestOptions which has an 'AdditionalRequestParameters' that might contain user defined types but that have already been annotated by 'TryAddAdditionalParameter'.")]
    internal static InteractiveRequestOptions FromState(string state) => JsonSerializer.Deserialize<InteractiveRequestOptions>(
        state,
        SerializerOptions);

    internal class Converter : JsonConverter<InteractiveRequestOptions>
    {
        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
            Justification = "This converter reads 'AdditionalRequestParameters' that might contain user defined types but that have already been annotated by 'TryAddAdditionalParameter'.")]
        public override InteractiveRequestOptions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var requestOptions = JsonSerializer.Deserialize<InteractiveOptions>(ref reader, options);

            return new InteractiveRequestOptions
            {
                AdditionalRequestParameters = requestOptions.AdditionalRequestParameters,
                Interaction = requestOptions.Interaction,
                ReturnUrl = requestOptions.ReturnUrl,
                Scopes = requestOptions.Scopes,
            };
        }

        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
            Justification = "This converter writes 'AdditionalRequestParameters' that might contain user defined types but that have already been annotated by 'TryAddAdditionalParameter'.")]
        public override void Write(Utf8JsonWriter writer, InteractiveRequestOptions value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(
                writer,
                new InteractiveOptions { ReturnUrl = value.ReturnUrl, Scopes = value.Scopes, Interaction = value.Interaction, AdditionalRequestParameters = value.AdditionalRequestParameters },
                options);
        }

        public struct InteractiveOptions
        {
            public string ReturnUrl { get; set; }

            public IEnumerable<string> Scopes { get; set; }

            public InteractionType Interaction { get; set; }

            public Dictionary<string, object> AdditionalRequestParameters { get; set; }
        }
    }
}
