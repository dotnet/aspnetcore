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
    /// <summary>
    /// Initializes a new instance of <see cref="InteractiveRequestOptions"/>.
    /// </summary>
    public InteractiveRequestOptions()
    {
    }

    /// <summary>
    /// Creates a new <see cref="InteractiveRequestOptions"/> for signing in with the given return url and scopes.
    /// </summary>
    /// <param name="returnUrl">The URL to return to after the interactive operation.</param>
    /// <param name="scopes">The scopes to request interactively.</param>
    /// <returns>An <see cref="InteractiveRequestOptions"/> configured for signing in.</returns>
    public static InteractiveRequestOptions SignIn(string returnUrl, IEnumerable<string> scopes = null)
    {
        return new InteractiveRequestOptions()
        {
            Interaction = InteractionType.SignIn,
            ReturnUrl = returnUrl,
            Scopes = scopes
        };
    }

    /// <summary>
    /// Creates a new <see cref="InteractiveRequestOptions"/> for signing out with the given return url.
    /// </summary>
    /// <param name="returnUrl">The URL to return to after the interactive operation.</param>
    /// <returns>An <see cref="InteractiveRequestOptions"/> configured for signing out.</returns>
    public static InteractiveRequestOptions SignOut(string returnUrl)
    {
        return new InteractiveRequestOptions
        {
            Interaction = InteractionType.SignOut,
            ReturnUrl = returnUrl,
            Scopes = null
        };
    }

    /// <summary>
    /// Creates a new <see cref="InteractiveRequestOptions"/> for signing in with the given return url and scopes.
    /// </summary>
    /// <param name="returnUrl">The URL to return to after the interactive operation.</param>
    /// <param name="scopes">The scopes to request interactively.</param>
    /// <returns>An <see cref="InteractiveRequestOptions"/> configured for requesting a token interactively.</returns>
    public static InteractiveRequestOptions GetToken(string returnUrl, IEnumerable<string> scopes = null)
    {
        return new InteractiveRequestOptions
        {
            Interaction = InteractionType.GetToken,
            ReturnUrl = returnUrl,
            Scopes = scopes
        };
    }

    /// <summary>
    /// Gets the redirect URL this request must return to upon successful completion.
    /// </summary>
    [JsonInclude]
    public string ReturnUrl { get; init; }

    /// <summary>
    /// Gets the scopes this request must use in the operation.
    /// </summary>
    [JsonInclude]
    public IEnumerable<string> Scopes { get; init; }

    /// <summary>
    /// Gets the request type.
    /// </summary>
    [JsonInclude]
    public InteractionType Interaction { get; init; }

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
    public bool TryRemoveAdditionalParameter<TValue>(string name)
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

        [UnconditionalSuppressMessageAttribute(
            "Trimming",
            "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
            Justification = "All the types serialized and deserialized have annotated APIs on the parent method that ensure the members are preserved.")]
        static TValue Deserialize(JsonElement element) => element.Deserialize<TValue>();
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Serializes 'InteractiveAuthenticationRequest' into a string")]
    internal string ToState() => JsonSerializer.Serialize(this);

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Deserializes InteractiveAuthenticationRequest")]
    internal static InteractiveRequestOptions FromState(string state) => JsonSerializer.Deserialize<InteractiveRequestOptions>(state);

    internal class Converter : JsonConverter<InteractiveRequestOptions>
    {
        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
            Justification = "Serializes InteractiveAuthenticationRequest")]
        public override InteractiveRequestOptions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var requestOptions = JsonSerializer.Deserialize<Options>(ref reader, options);

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
            Justification = "Serializes InteractiveAuthenticationRequest")]
        public override void Write(Utf8JsonWriter writer, InteractiveRequestOptions value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, new Options(value.ReturnUrl, value.Scopes, value.Interaction, value.AdditionalRequestParameters), options);
        }

        internal record struct Options(
            [property: JsonInclude] string ReturnUrl,
            [property: JsonInclude] IEnumerable<string> Scopes,
            [property: JsonInclude] InteractionType Interaction,
            [property: JsonInclude] Dictionary<string, object> AdditionalRequestParameters);
    }
}
