// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;

namespace Microsoft.AspNetCore.Authentication.OAuth
{
    /// <summary>
    /// Response from an provider for an OAuth token request.
    /// </summary>
    public class OAuthTokenResponse : IDisposable
    {
        /// <summary>
        /// Initializes a new isntance <see cref="OAuthTokenResponse"/>.
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
    }
}
