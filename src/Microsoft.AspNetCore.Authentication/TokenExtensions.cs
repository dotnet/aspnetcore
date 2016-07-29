// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features.Authentication;

namespace Microsoft.AspNetCore.Authentication
{
    public static class AuthenticationTokenExtensions
    {
        private static string TokenNamesKey = ".TokenNames";
        private static string TokenKeyPrefix = ".Token.";

        public static void StoreTokens(this AuthenticationProperties properties, IEnumerable<AuthenticationToken> tokens)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }
            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }

            // Clear old tokens first
            var oldTokens = properties.GetTokens();
            foreach (var t in oldTokens)
            {
                properties.Items.Remove(TokenKeyPrefix + t.Name);
            }
            properties.Items.Remove(TokenNamesKey);

            var tokenNames = new List<string>();
            foreach (var token in tokens)
            {
                // REVIEW: should probably check that there are no ; in the token name and throw or encode
                tokenNames.Add(token.Name);
                properties.Items[TokenKeyPrefix+token.Name] = token.Value;
            }
            if (tokenNames.Count > 0)
            {
                properties.Items[TokenNamesKey] = string.Join(";", tokenNames.ToArray());
            }
        }

        public static string GetTokenValue(this AuthenticationProperties properties, string tokenName)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }
            if (tokenName == null)
            {
                throw new ArgumentNullException(nameof(tokenName));
            }

            var tokenKey = TokenKeyPrefix + tokenName;
            return properties.Items.ContainsKey(tokenKey)
                ? properties.Items[tokenKey]
                : null;
        }

        public static bool UpdateTokenValue(this AuthenticationProperties properties, string tokenName, string tokenValue)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }
            if (tokenName == null)
            {
                throw new ArgumentNullException(nameof(tokenName));
            }

            var tokenKey = TokenKeyPrefix + tokenName;
            if (!properties.Items.ContainsKey(tokenKey))
            {
                return false;
            }
            properties.Items[tokenKey] = tokenValue;
            return true;
        }

        public static IEnumerable<AuthenticationToken> GetTokens(this AuthenticationProperties properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            var tokens = new List<AuthenticationToken>();
            if (properties.Items.ContainsKey(TokenNamesKey))
            {
                var tokenNames = properties.Items[TokenNamesKey].Split(';');
                foreach (var name in tokenNames)
                {
                    var token = properties.GetTokenValue(name);
                    if (token != null)
                    {
                        tokens.Add(new AuthenticationToken { Name = name, Value = token });
                    }
                }
            }

            return tokens;
        }

        public static Task<string> GetTokenAsync(this AuthenticationManager manager, string tokenName)
        {
            return manager.GetTokenAsync(AuthenticationManager.AutomaticScheme, tokenName);
        }

        public static async Task<string> GetTokenAsync(this AuthenticationManager manager, string signInScheme, string tokenName)
        {
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }
            if (signInScheme == null)
            {
                throw new ArgumentNullException(nameof(signInScheme));
            }
            if (tokenName == null)
            {
                throw new ArgumentNullException(nameof(tokenName));
            }

            var authContext = new AuthenticateContext(signInScheme);
            await manager.AuthenticateAsync(authContext);
            return new AuthenticationProperties(authContext.Properties).GetTokenValue(tokenName);
        }
    }
}