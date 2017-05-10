// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity.Service
{
    public class TokenValueDescriptor
    {
        public TokenValueDescriptor(string name, TokenValueCardinality cardinality)
            : this(name, name, cardinality)
        {
        }

        public TokenValueDescriptor(string name, string alias, TokenValueCardinality cardinality)
        {
            Name = name;
            Alias = alias;
            Cardinality = cardinality;
        }

        public string Name { get; }
        public string Alias { get; }
        public TokenValueCardinality Cardinality { get; }
    }
}
