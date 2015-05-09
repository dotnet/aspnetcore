// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Authentication.OAuth;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Authentication.MicrosoftAccount
{
    /// <summary>
    /// Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.
    /// </summary>
    public class MicrosoftAccountAuthenticatedContext : OAuthAuthenticatedContext
    {
        /// <summary>
        /// Initializes a new <see cref="MicrosoftAccountAuthenticatedContext"/>.
        /// </summary>
        /// <param name="context">The HTTP environment.</param>
        /// <param name="user">The JSON-serialized user.</param>
        /// <param name="tokens">The access token provided by the Microsoft authentication service.</param>
        public MicrosoftAccountAuthenticatedContext(HttpContext context, OAuthAuthenticationOptions options, [NotNull] JObject user, TokenResponse tokens)
            : base(context, options, user, tokens)
        {
            IDictionary<string, JToken> userAsDictionary = user;

            JToken userId = User["id"];
            if (userId == null)
            {
                throw new ArgumentException(Resources.Exception_MissingId, nameof(user));
            }

            Id = userId.ToString();
            Name = PropertyValueIfExists("name", userAsDictionary);
            FirstName = PropertyValueIfExists("first_name", userAsDictionary);
            LastName = PropertyValueIfExists("last_name", userAsDictionary);

            if (userAsDictionary.ContainsKey("emails"))
            {
                JToken emailsNode = user["emails"];
                foreach (var childAsProperty in emailsNode.OfType<JProperty>().Where(childAsProperty => childAsProperty.Name == "preferred"))
                {
                    Email = childAsProperty.Value.ToString();
                }
            }
        }

        /// <summary>
        /// Gets the Microsoft Account user ID.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets the user's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the user's first name.
        /// </summary>
        public string FirstName { get; private set; }

        /// <summary>
        /// Gets the user's last name.
        /// </summary>
        public string LastName { get; private set; }

        /// <summary>
        /// Gets the user's email address.
        /// </summary>
        public string Email { get; private set; }

        private static string PropertyValueIfExists(string property, IDictionary<string, JToken> dictionary)
        {
            return dictionary.ContainsKey(property) ? dictionary[property].ToString() : null;
        }
    }
}
