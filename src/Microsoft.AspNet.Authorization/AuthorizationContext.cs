// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Authorization
{
    /// <summary>
    /// Contains authorization information used by <see cref="IAuthorizationHandler"/>.
    /// </summary>
    public class AuthorizationContext
    {
        private HashSet<IAuthorizationRequirement> _pendingRequirements;
        private bool _failCalled;
        private bool _succeedCalled;

        public AuthorizationContext(
            [NotNull] IEnumerable<IAuthorizationRequirement> requirements, 
            ClaimsPrincipal user,
            object resource)
        {
            Requirements = requirements;
            User = user;
            Resource = resource;
            _pendingRequirements = new HashSet<IAuthorizationRequirement>(requirements);
        }

        public IEnumerable<IAuthorizationRequirement> Requirements { get; }
        public ClaimsPrincipal User { get; }
        public object Resource { get; }

        public IEnumerable<IAuthorizationRequirement> PendingRequirements { get { return _pendingRequirements; } }

        public bool HasFailed { get { return _failCalled; } }

        public bool HasSucceeded {
            get
            {
                return !_failCalled && _succeedCalled && !PendingRequirements.Any();
            }
        }

        public void Fail()
        {
            _failCalled = true;
        }

        public void Succeed(IAuthorizationRequirement requirement)
        {
            _succeedCalled = true;
            _pendingRequirements.Remove(requirement);
        }
    }
}