// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class DefaultTokenResponseParameterProvider : ITokenResponseParameterProvider
    {
        private readonly ITimeStampManager _manager;

        public int Order => 100;

        public DefaultTokenResponseParameterProvider(ITimeStampManager manager)
        {
            _manager = manager;
        }

        public Task AddParameters(TokenGeneratingContext context, OpenIdConnectMessage response)
        {
            if (context.IdToken != null)
            {
                response.IdToken = context.IdToken.SerializedValue;
                var expiresIn = _manager.GetDurationInSeconds(
                        context.IdToken.Token.Expires,
                        context.IdToken.Token.IssuedAt);

                response.Parameters.Add(
                    "id_token_expires_in",
                    expiresIn.ToString(CultureInfo.InvariantCulture));
            }

            if (context.AccessToken != null)
            {
                response.AccessToken = context.AccessToken.SerializedValue;
                response.ExpiresIn = GetExpirationTime(context.AccessToken.Token);
                response.Parameters["expires_on"] = context.AccessToken.Token.GetClaimValue(IdentityServiceClaimTypes.Expires);
                response.Parameters["not_before"] = context.AccessToken.Token.GetClaimValue(IdentityServiceClaimTypes.NotBefore);
                response.Resource = context.RequestGrants.Scopes.First(s => s.ClientId != null).ClientId;
            }

            if (context.RefreshToken != null)
            {
                response.RefreshToken = context.RefreshToken.SerializedValue;
                var expiresIn = _manager.GetDurationInSeconds(
                    context.RefreshToken.Token.Expires,
                    context.RefreshToken.Token.IssuedAt);

                response.Parameters.Add(
                    "refresh_token_expires_in",
                    expiresIn.ToString(CultureInfo.InvariantCulture));
            }

            response.TokenType = "Bearer";
            return Task.CompletedTask;
        }

        private static string GetExpirationTime(Token token)
        {
            if (token.Expires < token.IssuedAt)
            {
                throw new InvalidOperationException("Can't expire before issuance.");
            }

            var expirationTimeInSeconds = Math.Truncate((token.Expires - token.IssuedAt).TotalSeconds);
            checked
            {
                return ((long)expirationTimeInSeconds).ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
