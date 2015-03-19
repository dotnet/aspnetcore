// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Authentication.OAuth;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Authentication.Facebook
{
    /// <summary>
    /// Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.
    /// </summary>
    public class FacebookAuthenticatedContext : OAuthAuthenticatedContext
    {
        /// <summary>
        /// Initializes a new <see cref="FacebookAuthenticatedContext"/>.
        /// </summary>
        /// <param name="context">The HTTP environment.</param>
        /// <param name="user">The JSON-serialized user.</param>
        /// <param name="tokens">The Facebook Access token.</param>
        public FacebookAuthenticatedContext(HttpContext context, OAuthAuthenticationOptions options, JObject user, TokenResponse tokens)
            : base(context, options, user, tokens)
        {
            Id = TryGetValue(user, "id");
            Name = TryGetValue(user, "name");
            Link = TryGetValue(user, "link");
            UserName = TryGetValue(user, "username");
            Email = TryGetValue(user, "email");
        }

        /// <summary>
        /// Gets the Facebook user ID.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets the user's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the user's link.
        /// </summary>
        public string Link { get; private set; }

        /// <summary>
        /// Gets the Facebook username.
        /// </summary>
        public string UserName { get; private set; }

        /// <summary>
        /// Gets the Facebook email.
        /// </summary>
        public string Email { get; private set; }

        private static string TryGetValue(JObject user, string propertyName)
        {
            JToken value;
            return user.TryGetValue(propertyName, out value) ? value.ToString() : null;
        }
    }
}
