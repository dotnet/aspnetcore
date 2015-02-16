// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Microsoft.AspNet.Security
{
    /// <summary>
    /// Contains authorization information used by <see cref="IAuthorizationPolicyHandler"/>.
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

        public IEnumerable<IAuthorizationRequirement> Requirements { get; private set; }
        public ClaimsPrincipal User { get; private set; }
        public object Resource { get; private set; }

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