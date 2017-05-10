// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Identity.Service
{
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public class ApplicationScope : IEquatable<ApplicationScope>
    {
        public static readonly ApplicationScope OpenId = new ApplicationScope("openid");
        public static readonly ApplicationScope Profile = new ApplicationScope("profile");
        public static readonly ApplicationScope Email = new ApplicationScope("email");
        public static readonly ApplicationScope OfflineAccess = new ApplicationScope("offline_access");

        public static readonly IReadOnlyDictionary<string, ApplicationScope> CanonicalScopes = new Dictionary<string, ApplicationScope>(StringComparer.Ordinal)
        {
            [OpenId.Scope] = OpenId,
            [Profile.Scope] = Profile,
            [Email.Scope] = Email,
            [OfflineAccess.Scope] = OfflineAccess
        };

        private ApplicationScope(string scope)
        {
            Scope = scope;
        }

        public ApplicationScope(string clientId, string scope)
        {
            ClientId = clientId;
            Scope = scope;
        }

        public string ClientId { get; }
        public string Scope { get; }

        public bool Equals(ApplicationScope other) => string.Equals(ClientId, other?.ClientId, StringComparison.Ordinal) &&
            string.Equals(Scope, other?.Scope, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as ApplicationScope);

        public override int GetHashCode() => ClientId == null ? Scope.GetHashCode() : ClientId.GetHashCode() ^ Scope.GetHashCode();

        private string DebuggerDisplay() => ClientId != null ? $"{ClientId},{Scope}" : Scope;
    }
}
