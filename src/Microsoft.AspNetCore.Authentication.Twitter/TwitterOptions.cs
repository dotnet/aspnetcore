// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Twitter
{
    /// <summary>
    /// Options for the Twitter authentication handler.
    /// </summary>
    public class TwitterOptions : RemoteAuthenticationOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TwitterOptions"/> class.
        /// </summary>
        public TwitterOptions()
        {
            CallbackPath = new PathString("/signin-twitter");
            BackchannelTimeout = TimeSpan.FromSeconds(60);
            Events = new TwitterEvents();

            ClaimActions.MapJsonKey(ClaimTypes.Email, "email", ClaimValueTypes.Email);
        }

        /// <summary>
        /// Gets or sets the consumer key used to communicate with Twitter.
        /// </summary>
        /// <value>The consumer key used to communicate with Twitter.</value>
        public string ConsumerKey { get; set; }

        /// <summary>
        /// Gets or sets the consumer secret used to sign requests to Twitter.
        /// </summary>
        /// <value>The consumer secret used to sign requests to Twitter.</value>
        public string ConsumerSecret { get; set; }

        /// <summary>
        /// Enables the retrieval user details during the authentication process, including
        /// e-mail addresses. Retrieving e-mail addresses requires special permissions
        /// from Twitter Support on a per application basis. The default is false.
        /// See https://dev.twitter.com/rest/reference/get/account/verify_credentials
        /// </summary>
        public bool RetrieveUserDetails { get; set; }

        /// <summary>
        /// A collection of claim actions used to select values from the json user data and create Claims.
        /// </summary>
        public ClaimActionCollection ClaimActions { get; } = new ClaimActionCollection();

        /// <summary>
        /// Gets or sets the type used to secure data handled by the handler.
        /// </summary>
        public ISecureDataFormat<RequestToken> StateDataFormat { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TwitterEvents"/> used to handle authentication events.
        /// </summary>
        public new TwitterEvents Events
        {
            get { return (TwitterEvents)base.Events; }
            set { base.Events = value; }
        }
    }
}
