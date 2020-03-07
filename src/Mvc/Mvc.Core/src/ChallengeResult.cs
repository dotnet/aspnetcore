// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An <see cref="ActionResult"/> that on execution invokes <see cref="M:HttpContext.ChallengeAsync"/>.
    /// </summary>
    public class ChallengeResult : ActionResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ChallengeResult"/>.
        /// </summary>
        public ChallengeResult()
            : this(Array.Empty<string>())
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ChallengeResult"/> with the
        /// specified authentication scheme.
        /// </summary>
        /// <param name="authenticationScheme">The authentication scheme to challenge.</param>
        public ChallengeResult(string authenticationScheme)
            : this(new[] { authenticationScheme })
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ChallengeResult"/> with the
        /// specified authentication schemes.
        /// </summary>
        /// <param name="authenticationSchemes">The authentication schemes to challenge.</param>
        public ChallengeResult(IList<string> authenticationSchemes)
            : this(authenticationSchemes, properties: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ChallengeResult"/> with the
        /// specified <paramref name="properties"/>.
        /// </summary>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
        /// challenge.</param>
        public ChallengeResult(AuthenticationProperties properties)
            : this(Array.Empty<string>(), properties)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ChallengeResult"/> with the
        /// specified authentication scheme and <paramref name="properties"/>.
        /// </summary>
        /// <param name="authenticationScheme">The authentication schemes to challenge.</param>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
        /// challenge.</param>
        public ChallengeResult(string authenticationScheme, AuthenticationProperties properties)
            : this(new[] { authenticationScheme }, properties)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ChallengeResult"/> with the
        /// specified authentication schemes and <paramref name="properties"/>.
        /// </summary>
        /// <param name="authenticationSchemes">The authentication scheme to challenge.</param>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
        /// challenge.</param>
        public ChallengeResult(IList<string> authenticationSchemes, AuthenticationProperties properties)
        {
            AuthenticationSchemes = authenticationSchemes;
            Properties = properties;
        }

        /// <summary>
        /// Gets or sets the authentication schemes that are challenged.
        /// </summary>
        public IList<string> AuthenticationSchemes { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="AuthenticationProperties"/> used to perform the authentication challenge.
        /// </summary>
        public AuthenticationProperties Properties { get; set; }

        /// <inheritdoc />
        public override async Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<ChallengeResult>();

            logger.ChallengeResultExecuting(AuthenticationSchemes);

            if (AuthenticationSchemes != null && AuthenticationSchemes.Count > 0)
            {
                foreach (var scheme in AuthenticationSchemes)
                {
                    await context.HttpContext.ChallengeAsync(scheme, Properties);
                }
            }
            else
            {
                await context.HttpContext.ChallengeAsync(Properties);
            }
        }
    }
}
