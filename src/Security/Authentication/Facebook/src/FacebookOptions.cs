// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Facebook
{
    /// <summary>
    /// Configuration options for <see cref="FacebookHandler"/>.
    /// </summary>
    public class FacebookOptions : OAuthOptions
    {
        /// <summary>
        /// Initializes a new <see cref="FacebookOptions"/>.
        /// </summary>
        public FacebookOptions()
        {
            CallbackPath = new PathString("/signin-facebook");
            SendAppSecretProof = true;
            AuthorizationEndpoint = FacebookDefaults.AuthorizationEndpoint;
            TokenEndpoint = FacebookDefaults.TokenEndpoint;
            UserInformationEndpoint = FacebookDefaults.UserInformationEndpoint;
            Scope.Add("email");
            Fields.Add("name");
            Fields.Add("email");
            Fields.Add("first_name");
            Fields.Add("last_name");

            ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
            ClaimActions.MapJsonSubKey("urn:facebook:age_range_min", "age_range", "min");
            ClaimActions.MapJsonSubKey("urn:facebook:age_range_max", "age_range", "max");
            ClaimActions.MapJsonKey(ClaimTypes.DateOfBirth, "birthday");
            ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
            ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
            ClaimActions.MapJsonKey(ClaimTypes.GivenName, "first_name");
            ClaimActions.MapJsonKey("urn:facebook:middle_name", "middle_name");
            ClaimActions.MapJsonKey(ClaimTypes.Surname, "last_name");
            ClaimActions.MapJsonKey(ClaimTypes.Gender, "gender");
            ClaimActions.MapJsonKey("urn:facebook:link", "link");
            ClaimActions.MapJsonSubKey("urn:facebook:location", "location", "name");
            ClaimActions.MapJsonKey(ClaimTypes.Locality, "locale");
            ClaimActions.MapJsonKey("urn:facebook:timezone", "timezone");
        }

        /// <summary>
        /// Check that the options are valid.  Should throw an exception if things are not ok.
        /// </summary>
        public override void Validate()
        {
            if (string.IsNullOrEmpty(AppId))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Exception_OptionMustBeProvided, nameof(AppId)), nameof(AppId));
            }

            if (string.IsNullOrEmpty(AppSecret))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Exception_OptionMustBeProvided, nameof(AppSecret)), nameof(AppSecret));
            }

            base.Validate();
        }

        // Facebook uses a non-standard term for this field.
        /// <summary>
        /// Gets or sets the Facebook-assigned App ID.
        /// </summary>
        public string AppId
        {
            get { return ClientId; }
            set { ClientId = value; }
        }

        // Facebook uses a non-standard term for this field.
        /// <summary>
        /// Gets or sets the Facebook-assigned app secret.
        /// </summary>
        public string AppSecret
        {
            get { return ClientSecret; }
            set { ClientSecret = value; }
        }

        /// <summary>
        /// Gets or sets if the <c>appsecret_proof</c> should be generated and sent with Facebook API calls.
        /// </summary>
        /// <remarks>See https://developers.facebook.com/docs/graph-api/securing-requests/#appsecret_proof for more details.</remarks>
        /// <value>Defaults to <see langword="true"/>.</value>
        public bool SendAppSecretProof { get; set; }

        /// <summary>
        /// The list of fields to retrieve from the UserInformationEndpoint.
        /// https://developers.facebook.com/docs/graph-api/reference/user
        /// </summary>
        /// <value>
        /// Defaults to include the following fields if none are specified: "name", "email", "first_name", and "last_name".
        /// </value>
        public ICollection<string> Fields { get; } = new HashSet<string>();
    }
}
