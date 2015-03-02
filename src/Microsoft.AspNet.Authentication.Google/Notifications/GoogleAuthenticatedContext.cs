// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Net.Http;
using System.Security.Claims;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Authentication.OAuth;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Authentication.Google
{
    /// <summary>
    /// Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.
    /// </summary>
    public class GoogleAuthenticatedContext : OAuthAuthenticatedContext
    {
        /// <summary>
        /// Initializes a new <see cref="GoogleAuthenticatedContext"/>.
        /// </summary>
        /// <param name="context">The HTTP environment.</param>
        /// <param name="user">The JSON-serialized Google user info.</param>
        /// <param name="tokens">Google OAuth 2.0 access token, refresh token, etc.</param>
        public GoogleAuthenticatedContext(HttpContext context, OAuthAuthenticationOptions options, JObject user, TokenResponse tokens)
            : base(context, options, user, tokens)
        {
            Id = TryGetValue(user, "id");
            Name = TryGetValue(user, "displayName");
            GivenName = TryGetValue(user, "name", "givenName");
            FamilyName = TryGetValue(user, "name", "familyName");
            Profile = TryGetValue(user, "url");
            Email = TryGetFirstValue(user, "emails", "value");
        }

        /// <summary>
        /// Gets the Google user ID.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets the user's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the user's given name.
        /// </summary>
        public string GivenName { get; set; }

        /// <summary>
        /// Gets the user's family name.
        /// </summary>
        public string FamilyName { get; set; }

        /// <summary>
        /// Gets the user's profile link.
        /// </summary>
        public string Profile { get; private set; }

        /// <summary>
        /// Gets the user's email.
        /// </summary>
        public string Email { get; private set; }

        private static string TryGetValue(JObject user, string propertyName)
        {
            JToken value;
            return user.TryGetValue(propertyName, out value) ? value.ToString() : null;
        }

        // Get the given subProperty from a property.
        private static string TryGetValue(JObject user, string propertyName, string subProperty)
        {
            JToken value;
            if (user.TryGetValue(propertyName, out value))
            {
                var subObject = JObject.Parse(value.ToString());
                if (subObject != null && subObject.TryGetValue(subProperty, out value))
                {
                    return value.ToString();
                }
            }
            return null;
        }

        // Get the given subProperty from a list property.
        private static string TryGetFirstValue(JObject user, string propertyName, string subProperty)
        {
            JToken value;
            if (user.TryGetValue(propertyName, out value))
            {
                var array = JArray.Parse(value.ToString());
                if (array != null && array.Count > 0)
                {
                    var subObject = JObject.Parse(array.First.ToString());
                    if (subObject != null)
                    {
                        if (subObject.TryGetValue(subProperty, out value))
                        {
                            return value.ToString();
                        }
                    }
                }
            }
            return null;
        }
    }
}
