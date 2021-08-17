// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Authorization
{
    /// <summary>
    /// Encapsulates a reason why authorization failed.
    /// </summary>
    public class AuthorizationFailureReason
    {
        /// <summary>
        /// Creates a new failure reason.
        /// </summary>
        /// <param name="handler">The handler responsible for this failure reason.</param>
        /// <param name="message">The message describing the failure.</param>
        public AuthorizationFailureReason(IAuthorizationHandler handler, string message)
        {
            Handler = handler;
            Message = message;
        }

        /// <summary>
        /// A message describing the failure reason.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The <see cref="IAuthorizationHandler"/> responsible for this failure reason.
        /// </summary>
        public IAuthorizationHandler Handler { get; set; }
    }

    /// <summary>
    /// Encapsulates a failure result of <see cref="IAuthorizationService.AuthorizeAsync(ClaimsPrincipal, object, IEnumerable{IAuthorizationRequirement})"/>.
    /// </summary>
    public class AuthorizationFailure
    {
        private AuthorizationFailure() { }

        /// <summary>
        /// Failure was due to <see cref="AuthorizationHandlerContext.Fail()"/> being called.
        /// </summary>
        public bool FailCalled { get; private set; }

        /// <summary>
        /// Failure was due to these requirements not being met via <see cref="AuthorizationHandlerContext.Succeed(IAuthorizationRequirement)"/>.
        /// </summary>
        public IEnumerable<IAuthorizationRequirement> FailedRequirements { get; private set; } = Array.Empty<IAuthorizationRequirement>();

        /// <summary>
        /// Allows <see cref="IAuthorizationHandler"/> to flow more detailed reasons for why authorization failed.
        /// </summary>
        public IEnumerable<AuthorizationFailureReason> Reasons { get; private set; } = Array.Empty<AuthorizationFailureReason>();

        /// <summary>
        /// Return a failure due to <see cref="AuthorizationHandlerContext.Fail()"/> being called.
        /// </summary>
        /// <returns>The failure.</returns>
        public static AuthorizationFailure ExplicitFail()
            => new AuthorizationFailure
            {
                FailCalled = true
            };

        /// <summary>
        /// Return a failure due to <see cref="AuthorizationHandlerContext.Fail(AuthorizationFailureReason)"/> being called.
        /// </summary>
        /// <returns>The failure.</returns>
        public static AuthorizationFailure Failed(IEnumerable<AuthorizationFailureReason> reasons)
            => new AuthorizationFailure
            {
                FailCalled = true,
                Reasons = reasons
            };

        /// <summary>
        /// Return a failure due to some requirements not being met via <see cref="AuthorizationHandlerContext.Succeed(IAuthorizationRequirement)"/>.
        /// </summary>
        /// <param name="failed">The requirements that were not met.</param>
        /// <returns>The failure.</returns>
        public static AuthorizationFailure Failed(IEnumerable<IAuthorizationRequirement> failed)
            => new AuthorizationFailure { FailedRequirements = failed };

    }
}
