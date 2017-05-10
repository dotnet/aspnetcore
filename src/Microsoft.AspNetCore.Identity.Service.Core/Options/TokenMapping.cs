// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class TokenMapping : Collection<TokenValueDescriptor>
    {
        public TokenMapping(string source)
        {
            Source = source;
        }

        public string Source { get; }

        public void AddSingle(string claimType, string contextKey)
        {
            Add(new TokenValueDescriptor(claimType, contextKey, TokenValueCardinality.One));
        }

        public void AddSingle(string name)
        {
            Add(new TokenValueDescriptor(name, TokenValueCardinality.One));
        }
    }
}
