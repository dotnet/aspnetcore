// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An <see cref="ActionResult"/> that on execution invokes <see cref="M:AuthenticationManager.ForbidAsync"/>.
    /// </summary>
    public class ForbidResult : ActionResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ForbidResult"/>.
        /// </summary>
        public ForbidResult()
            : this(new string[] { })
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ForbidResult"/> with the
        /// specified authentication scheme.
        /// </summary>
        /// <param name="authenticationScheme">The authentication scheme to challenge.</param>
        public ForbidResult(string authenticationScheme)
            : this(new[] { authenticationScheme })
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ForbidResult"/> with the
        /// specified authentication schemes.
        /// </summary>
        /// <param name="authenticationSchemes">The authentication schemes to challenge.</param>
        public ForbidResult(IList<string> authenticationSchemes)
            : this(authenticationSchemes, properties: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ForbidResult"/> with the
        /// specified <paramref name="properties"/>.
        /// </summary>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
        /// challenge.</param>
        public ForbidResult(AuthenticationProperties properties)
            : this(new string[] { }, properties)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ForbidResult"/> with the
        /// specified authentication scheme and <paramref name="properties"/>.
        /// </summary>
        /// <param name="authenticationScheme">The authentication schemes to challenge.</param>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
        /// challenge.</param>
        public ForbidResult(string authenticationScheme, AuthenticationProperties properties)
            : this(new[] { authenticationScheme }, properties)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ForbidResult"/> with the
        /// specified authentication schemes and <paramref name="properties"/>.
        /// </summary>
        /// <param name="authenticationSchemes">The authentication scheme to challenge.</param>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
        /// challenge.</param>
        public ForbidResult(IList<string> authenticationSchemes, AuthenticationProperties properties)
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
            var logger = loggerFactory.CreateLogger<ForbidResult>();

            logger.ForbidResultExecuting(AuthenticationSchemes);

            var authentication = context.HttpContext.Authentication;

            if (AuthenticationSchemes != null && AuthenticationSchemes.Count > 0)
            {
                for (var i = 0; i < AuthenticationSchemes.Count; i++)
                {
                    await authentication.ForbidAsync(AuthenticationSchemes[i], Properties);
                }
            }
            else
            {
                await authentication.ForbidAsync(Properties);
            }
        }
    }
}
