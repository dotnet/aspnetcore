// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An <see cref="ActionResult"/> that on execution invokes <see cref="M:HttpContext.SignOutAsync"/>.
    /// </summary>
    public class SignOutResult : ActionResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SignOutResult"/> with the default sign out scheme.
        /// </summary>
        public SignOutResult()
            : this(Array.Empty<string>())
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SignOutResult"/> with the
        /// specified authentication scheme.
        /// </summary>
        /// <param name="authenticationScheme">The authentication scheme to use when signing out the user.</param>
        public SignOutResult(string authenticationScheme)
            : this(new[] { authenticationScheme })
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SignOutResult"/> with the
        /// specified authentication schemes.
        /// </summary>
        /// <param name="authenticationSchemes">The authentication schemes to use when signing out the user.</param>
        public SignOutResult(IList<string> authenticationSchemes)
            : this(authenticationSchemes, properties: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SignOutResult"/> with the
        /// specified authentication scheme and <paramref name="properties"/>.
        /// </summary>
        /// <param name="authenticationScheme">The authentication schemes to use when signing out the user.</param>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the sign-out operation.</param>
        public SignOutResult(string authenticationScheme, AuthenticationProperties properties)
            : this(new[] { authenticationScheme }, properties)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SignOutResult"/> with the
        /// specified authentication schemes and <paramref name="properties"/>.
        /// </summary>
        /// <param name="authenticationSchemes">The authentication scheme to use when signing out the user.</param>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the sign-out operation.</param>
        public SignOutResult(IList<string> authenticationSchemes, AuthenticationProperties properties)
        {
            AuthenticationSchemes = authenticationSchemes ?? throw new ArgumentNullException(nameof(authenticationSchemes));
            Properties = properties;
        }

        /// <summary>
        /// Gets or sets the authentication schemes that are challenged.
        /// </summary>
        public IList<string> AuthenticationSchemes { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="AuthenticationProperties"/> used to perform the sign-out operation.
        /// </summary>
        public AuthenticationProperties Properties { get; set; }

        /// <inheritdoc />
        public override async Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (AuthenticationSchemes == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatPropertyOfTypeCannotBeNull(
                        /* property: */ nameof(AuthenticationSchemes),
                        /* type: */ nameof(SignOutResult)));
            }

            var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<SignOutResult>();

            logger.SignOutResultExecuting(AuthenticationSchemes);

            if (AuthenticationSchemes.Count == 0)
            {
                await context.HttpContext.SignOutAsync(Properties);
            }
            else
            {
                for (var i = 0; i < AuthenticationSchemes.Count; i++)
                {
                    await context.HttpContext.SignOutAsync(AuthenticationSchemes[i], Properties);
                }
            }
        }
    }
}
