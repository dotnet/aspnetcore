// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class DefaultAuthorizationResponseParameterProvider : IAuthorizationResponseParameterProvider
    {
        private readonly ITimeStampManager _manager;

        public int Order => 100;

        public DefaultAuthorizationResponseParameterProvider(ITimeStampManager manager)
        {
            _manager = manager;
        }

        public Task AddParameters(TokenGeneratingContext context, AuthorizationResponse response)
        {
            if (context.AuthorizationCode != null)
            {
                response.Message.Code = context.AuthorizationCode.SerializedValue;
            }

            if (context.AccessToken != null)
            {
                response.Message.AccessToken = context.AccessToken.SerializedValue;
                response.Message.TokenType = "Bearer";
                response.Message.ExpiresIn = GetExpirationTime(context.AccessToken.Token);
                response.Message.Scope = string.Join(" ", context.RequestGrants.Scopes.Select(s => s.Scope));
            }

            if (context.IdToken != null)
            {
                response.Message.IdToken = context.IdToken.SerializedValue;
            }

            response.Message.State = context.RequestParameters.State;
            return Task.CompletedTask;
        }

        private string GetExpirationTime(Token token)
        {
            if (token.Expires < token.IssuedAt)
            {
                throw new InvalidOperationException("Can't expire before issuance.");
            }

            return _manager.GetDurationInSeconds(token.Expires, token.IssuedAt).ToString(CultureInfo.InvariantCulture);
        }
    }
}
