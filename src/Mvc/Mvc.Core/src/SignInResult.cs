// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An <see cref="ActionResult"/> that on execution invokes <see cref="M:HttpContext.SignInAsync"/>.
    /// </summary>
    public class SignInResult : ActionResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SignInResult"/> with the
        /// specified authentication scheme.
        /// </summary>
        /// <param name="authenticationScheme">The authentication scheme to use when signing in the user.</param>
        /// <param name="principal">The claims principal containing the user claims.</param>
        public SignInResult(string authenticationScheme, ClaimsPrincipal principal)
            : this(authenticationScheme, principal, properties: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SignInResult"/> with the
        /// specified authentication scheme and <paramref name="properties"/>.
        /// </summary>
        /// <param name="authenticationScheme">The authentication schemes to use when signing in the user.</param>
        /// <param name="principal">The claims principal containing the user claims.</param>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the sign-in operation.</param>
        public SignInResult(string authenticationScheme, ClaimsPrincipal principal, AuthenticationProperties properties)
        {
            AuthenticationScheme = authenticationScheme ?? throw new ArgumentNullException(nameof(authenticationScheme));
            Principal = principal ?? throw new ArgumentNullException(nameof(principal));
            Properties = properties;
        }

        /// <summary>
        /// Gets or sets the authentication scheme that is used to perform the sign-in operation.
        /// </summary>
        public string AuthenticationScheme { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ClaimsPrincipal"/> containing the user claims.
        /// </summary>
        public ClaimsPrincipal Principal { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="AuthenticationProperties"/> used to perform the sign-in operation.
        /// </summary>
        public AuthenticationProperties Properties { get; set; }

        /// <inheritdoc />
        public override async Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (AuthenticationScheme == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatPropertyOfTypeCannotBeNull(
                        /* property: */ nameof(AuthenticationScheme),
                        /* type: */ nameof(SignInResult)));
            }

            var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<SignInResult>();

            logger.SignInResultExecuting(AuthenticationScheme, Principal);

            await context.HttpContext.SignInAsync(AuthenticationScheme, Principal, Properties);
        }
    }
}
