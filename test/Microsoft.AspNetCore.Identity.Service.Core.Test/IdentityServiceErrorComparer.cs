// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class IdentityServiceErrorComparer : IEqualityComparer<AuthorizationRequestError>,
        IEqualityComparer<OpenIdConnectMessage>
    {
        public static IdentityServiceErrorComparer Instance { get; } = new IdentityServiceErrorComparer();

        public bool Equals(AuthorizationRequestError left, AuthorizationRequestError right)
        {
            return (left == null && right == null) || left != null && right != null &&
                string.Equals(left.Message.Error, right.Message.Error, StringComparison.Ordinal) &&
                string.Equals(left.Message.ErrorDescription, right.Message.ErrorDescription, StringComparison.Ordinal) &&
                string.Equals(left.Message.ErrorUri, right.Message.ErrorUri, StringComparison.Ordinal) &&
                string.Equals(left.Message.State, right.Message.State, StringComparison.Ordinal);
        }

        public bool Equals(OpenIdConnectMessage left, OpenIdConnectMessage right)
        {
            return (left == null && right == null) || left != null && right != null &&
                string.Equals(left.Error, right.Error, StringComparison.Ordinal) &&
                string.Equals(left.ErrorDescription, right.ErrorDescription, StringComparison.Ordinal) &&
                string.Equals(left.ErrorUri, right.ErrorUri, StringComparison.Ordinal) &&
                string.Equals(left.State, right.State, StringComparison.Ordinal);
        }

        public int GetHashCode(AuthorizationRequestError obj)
        {
            return 1; // Minimal implementation that satisfies the contract.
        }

        public int GetHashCode(OpenIdConnectMessage obj)
        {
            return 1; // Minimal implementation that satisfies the contract.
        }
    }
}
