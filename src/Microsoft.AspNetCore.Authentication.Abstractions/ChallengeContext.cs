// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Context used for challenges.
    /// </summary>
    public class ChallengeContext : BaseAuthenticationContext
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="httpContext">The context.</param>
        /// <param name="authenticationScheme">The name of the scheme.</param>
        public ChallengeContext(HttpContext httpContext, string authenticationScheme)
            : this(httpContext, authenticationScheme, properties: null, behavior: ChallengeBehavior.Automatic)
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="httpContext">The context.</param>
        /// <param name="authenticationScheme">The name of the scheme.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="behavior">The challenge behavior.</param>
        public ChallengeContext(HttpContext httpContext, string authenticationScheme, AuthenticationProperties properties, ChallengeBehavior behavior)
            : base(httpContext, authenticationScheme, properties)
        {
            if (string.IsNullOrEmpty(authenticationScheme))
            {
                throw new ArgumentException(nameof(authenticationScheme));
            }
            Behavior = behavior;
        }

        /// <summary>
        /// The challenge behavior.
        /// </summary>
        public ChallengeBehavior Behavior { get; }
    }
}