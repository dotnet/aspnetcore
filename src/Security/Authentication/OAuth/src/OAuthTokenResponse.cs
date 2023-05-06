// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;

namespace Microsoft.AspNetCore.Authentication.OAuth;

/// <summary>
/// Response from an provider for an OAuth token request.
/// </summary>
public class OAuthTokenResponse : IDisposable
{
    /// <summary>
    /// Initializes a new instance <see cref="OAuthTokenResponse"/>.
    /// </summary>
    /// <param name="response">The received JSON payload.</param>
    private OAuthTokenResponse(JsonDocument response)
    {
        Response = response;
        var root = response.RootElement;
        AccessToken = root.GetString("access_token");
        TokenType = root.GetString("token_type");
        RefreshToken = root.GetString("refresh_token");
        ExpiresIn = root.GetString("expires_in");
        Error = GetStandardErrorException(response);
    }

    private OAuthTokenResponse(Exception error)
    {
        Error = error;
    }

    /// <summary>
    /// Creates a successful <see cref="OAuthTokenResponse"/>.
    /// </summary>
    /// <param name="response">The received JSON payload.</param>
    /// <returns>A <see cref="OAuthTokenResponse"/> instance.</returns>
    public static OAuthTokenResponse Success(JsonDocument response)
    {
        return new OAuthTokenResponse(response);
    }

    /// <summary>
    /// Creates a failed <see cref="OAuthTokenResponse"/>.
    /// </summary>
    /// <param name="error">The error associated with the failure.</param>
    /// <returns>A <see cref="OAuthTokenResponse"/> instance.</returns>
    public static OAuthTokenResponse Failed(Exception error)
    {
        return new OAuthTokenResponse(error);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Response?.Dispose();
    }

    /// <summary>
    /// Gets or sets the received JSON payload.
    /// </summary>
    public JsonDocument? Response { get; set; }

    /// <summary>
    /// Gets or sets the access token issued by the OAuth provider.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the token type.
    /// </summary>
    /// <remarks>
    /// Typically the string “bearer”.
    /// </remarks>
    public string? TokenType { get; set; }

    /// <summary>
    /// Gets or sets a refresh token that applications can use to obtain another access token if tokens can expire.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the validatity lifetime of the token in seconds.
    /// </summary>
    public string? ExpiresIn { get; set; }

    /// <summary>
    /// The exception in the event the response was a failure.
    /// </summary>
    public Exception? Error { get; set; }

    internal static Exception? GetStandardErrorException(JsonDocument response)
    {
        var root = response.RootElement;
        var error = root.GetString("error");

        if (error is null)
        {
            return null;
        }

        var result = new StringBuilder("OAuth token endpoint failure: ");
        result.Append(error);

        if (root.TryGetProperty("error_description", out var errorDescription))
        {
            result.Append(";Description=");
            result.Append(errorDescription);
        }

        if (root.TryGetProperty("error_uri", out var errorUri))
        {
            result.Append(";Uri=");
            result.Append(errorUri);
        }

        var exception = new AuthenticationFailureException(result.ToString());
        exception.Data["error"] = error.ToString();
        exception.Data["error_description"] = errorDescription.ToString();
        exception.Data["error_uri"] = errorUri.ToString();

        return exception;
    }
}
