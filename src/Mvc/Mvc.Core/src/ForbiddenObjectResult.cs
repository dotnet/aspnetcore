// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An <see cref="ObjectResult"/> that when executed will produce a Forbidden(403) response.
    /// </summary>
    [DefaultStatusCode(DefaultStatusCode)]
    public class ForbiddenObjectResult : ObjectResult
    {
        private const int DefaultStatusCode = StatusCodes.Status403Forbidden;

        /// <summary>
        /// Creates a new <see cref="ForbiddenObjectResult"/> instance.
        /// </summary>
        public ForbiddenObjectResult([ActionResultObjectValue] object value) : base(value)
        {
            StatusCode = DefaultStatusCode;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ForbiddenObjectResult"/> with the
        /// specified authentication scheme.
        /// </summary>
        /// <param name="authenticationScheme">The authentication scheme to challenge.</param>
        /// <param name="value">Response payload.</param>
        public ForbiddenObjectResult(object value, string authenticationScheme)
            : this(value, new[] { authenticationScheme })
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ForbiddenObjectResult"/> with the
        /// specified authentication schemes.
        /// </summary>
        /// <param name="authenticationSchemes">The authentication schemes to challenge.</param>
        /// <param name="value">Response payload.</param>
        public ForbiddenObjectResult(object value, IList<string> authenticationSchemes)
            : this(value, authenticationSchemes, properties: null)
        {
        }
        /// <summary>
        /// Initializes a new instance of <see cref="ForbiddenObjectResult"/> with the
        /// specified <paramref name="properties"/>.
        /// </summary>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
        /// challenge.</param>
        /// <param name="value">Response payload.</param>
        public ForbiddenObjectResult(object value, AuthenticationProperties properties)
            : this(value, Array.Empty<string>(), properties)
        {
        }
        /// <summary>
        /// Initializes a new instance of <see cref="ForbiddenObjectResult"/> with the
        /// specified authentication scheme and <paramref name="properties"/>.
        /// </summary>
        /// <param name="authenticationScheme">The authentication schemes to challenge.</param>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
        /// challenge.</param>
        /// <param name="value">Response payload.</param>
        public ForbiddenObjectResult(object value, string authenticationScheme, AuthenticationProperties properties)
            : this(value, new[] { authenticationScheme }, properties)
        {
        }
        /// <summary>
        /// Initializes a new instance of <see cref="ForbiddenObjectResult"/> with the
        /// specified authentication schemes and <paramref name="properties"/>.
        /// </summary>
        /// <param name="authenticationSchemes">The authentication scheme to challenge.</param>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
        /// challenge.</param>
        /// <param name="value">Response payload.</param>
        public ForbiddenObjectResult(object value, IList<string> authenticationSchemes, AuthenticationProperties properties)
            : base(value)
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

        public override Task ExecuteResultAsync(ActionContext context)
        {
            var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<ForbiddenObjectResult>>();
            return executor.ExecuteAsync(context, this);
        }

    }
}