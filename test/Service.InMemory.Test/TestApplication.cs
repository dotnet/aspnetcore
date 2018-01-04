// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Identity.Service.InMemory.Test
{
    public class TestApplication
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string Name { get; set; }
        public string ClientSecretHash { get; set; }
        public IList<TestApplicationClaim> Claims { get; set; } = new List<TestApplicationClaim>();
        public IList<TestApplicationScope> Scopes { get; set; } = new List<TestApplicationScope>();
        public IList<TestApplicationRedirectUri> RedirectUris { get; set; } = new List<TestApplicationRedirectUri>();
    }

    public class TestUser
    {
    }

    public class TestApplicationClaim
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }

    public class TestApplicationScope
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }

    public class TestApplicationRedirectUri
    {
        public string Id { get; set; }
        public string Value { get; set; }
        public bool IsLogout { get; set; }
    }
}
