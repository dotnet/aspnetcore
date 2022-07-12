// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Represents the request to the identity provider for logging in or provisioning a token.
/// </summary>
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
    public static InteractiveRequestOptions SignIn(string returnUrl, IEnumerable<string> scopes)
    {
        return new InteractiveRequestOptions()
        {
            Interaction = InteractionType.SignIn,
            ReturnUrl = returnUrl,
            Scopes = scopes
        };
    }

    /// <summary>
    /// Creates a new <see cref="InteractiveRequestOptions"/> for signing in with the given return url.
    /// </summary>
    /// <param name="returnUrl">The URL to return to after the interactive operation.</param>
    /// <returns>An <see cref="InteractiveRequestOptions"/> configured for signing in.</returns>
    public static InteractiveRequestOptions SignIn(string returnUrl)
    {
        return new InteractiveRequestOptions
        {
            Interaction = InteractionType.SignIn,
            ReturnUrl = returnUrl,
            Scopes = null
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
    public static InteractiveRequestOptions GetToken(string returnUrl, IEnumerable<string> scopes)
    {
        return new InteractiveRequestOptions
        {
            Interaction = InteractionType.GetToken,
            ReturnUrl = returnUrl,
            Scopes = scopes
        };
    }

    /// <summary>
    /// Creates a new <see cref="InteractiveRequestOptions"/> for signing in with the given return url and the default scopes.
    /// </summary>
    /// <param name="returnUrl">The URL to return to after the interactive operation.</param>
    /// <returns>An <see cref="InteractiveRequestOptions"/> configured for requesting a token interactively.</returns>
    public static InteractiveRequestOptions GetToken(string returnUrl)
    {
        return new InteractiveRequestOptions
        {
            Interaction = InteractionType.GetToken,
            ReturnUrl = returnUrl,
            Scopes = null
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

    /// <summary>
    /// Gets or sets the additional parameters to pass in to the underlying provider.
    /// </summary>
    /// <remarks>
    /// The underlying provider is free to apply these parameters as it sees fit or ignore them completely. In the default
    /// implementations the parameters will be JSON serialized using System.Text.Json and passed as a parameter to the
    /// underlying JavaScript implementation that handles the operation details.
    /// </remarks>
    public IDictionary<string, object> AdditionalRequestParameters { get; set; } = new Dictionary<string, object>();

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Serializes 'InteractiveAuthenticationRequest' into a string")]
    internal string ToState() => JsonSerializer.Serialize(this);

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Deserializes InteractiveAuthenticationRequestRecord")]
    internal static InteractiveRequestOptions FromState(string state) => JsonSerializer.Deserialize<InteractiveRequestOptions>(state);
}
