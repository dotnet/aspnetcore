// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Extension methods for storing authentication tokens in <see cref="AuthenticationProperties"/>.
    /// </summary>
    public static class AuthenticationTokenExtensions
    {
        private static string TokenNamesKey = ".TokenNames";
        private static string TokenKeyPrefix = ".Token.";

        /// <summary>
        /// Stores a set of authentication tokens, after removing any old tokens.
        /// </summary>
        /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
        /// <param name="tokens">The tokens to store.</param>
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

        /// <summary>
        /// Returns the value of a token.
        /// </summary>
        /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
        /// <param name="tokenName">The token name.</param>
        /// <returns>The token value.</returns>
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

        /// <summary>
        /// Returns all of the AuthenticationTokens contained in the properties.
        /// </summary>
        /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
        /// <returns>The authentication tokens.</returns>
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

        /// <summary>
        /// Extension method for getting the value of an authentication token.
        /// </summary>
        /// <param name="auth">The <see cref="IAuthenticationService"/>.</param>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="tokenName">The name of the token.</param>
        /// <returns>The value of the token.</returns>
        public static Task<string> GetTokenAsync(this IAuthenticationService auth, HttpContext context, string tokenName)
            => auth.GetTokenAsync(context, scheme: null, tokenName: tokenName);

        /// <summary>
        /// Extension method for getting the value of an authentication token.
        /// </summary>
        /// <param name="auth">The <see cref="IAuthenticationService"/>.</param>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="scheme">The name of the authentication scheme.</param>
        /// <param name="tokenName">The name of the token.</param>
        /// <returns>The value of the token.</returns>
        public static async Task<string> GetTokenAsync(this IAuthenticationService auth, HttpContext context, string scheme, string tokenName)
        {
            if (auth == null)
            {
                throw new ArgumentNullException(nameof(auth));
            }
            if (tokenName == null)
            {
                throw new ArgumentNullException(nameof(tokenName));
            }

            var result = await auth.AuthenticateAsync(context, scheme);
            return result?.Properties?.GetTokenValue(tokenName);
        }
    }
}