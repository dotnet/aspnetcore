// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class IdentityServiceApplication : IdentityServiceApplication<string>
    {
    }

    public class IdentityServiceApplication<TUserKey> :
        IdentityServiceApplication<string, TUserKey>
        where TUserKey : IEquatable<TUserKey>
    {
    }

    public class IdentityServiceApplication<TApplicationKey, TUserKey> :
        IdentityServiceApplication<
            TApplicationKey,
            TUserKey,
            IdentityServiceScope<TApplicationKey>,
            IdentityServiceApplicationClaim<TApplicationKey>,
            IdentityServiceRedirectUri<TApplicationKey>>
        where TApplicationKey : IEquatable<TApplicationKey>
        where TUserKey : IEquatable<TUserKey>
    {
    }

    public class IdentityServiceApplication<TKey, TUserKey, TScope, TApplicationClaim, TRedirectUri>
        where TKey : IEquatable<TKey>
        where TUserKey : IEquatable<TUserKey>
        where TScope : IdentityServiceScope<TKey>
        where TApplicationClaim : IdentityServiceApplicationClaim<TKey>
        where TRedirectUri : IdentityServiceRedirectUri<TKey>
    {
        public TKey Id { get; set; }
        public string Name { get; set; }
        public TUserKey UserId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecretHash { get; set; }
        public string ConcurrencyStamp { get; set; }
        public ICollection<TScope> Scopes { get; set; }
        public ICollection<TApplicationClaim> Claims { get; set; }
        public ICollection<TRedirectUri> RedirectUris { get; set; }
    }
}
