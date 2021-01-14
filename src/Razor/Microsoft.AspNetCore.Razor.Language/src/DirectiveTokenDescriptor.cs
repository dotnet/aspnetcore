// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class DirectiveTokenDescriptor
    {
        public abstract DirectiveTokenKind Kind { get; }

        public abstract bool Optional { get; }

        public virtual string Name { get; }

        public virtual string Description { get; }

        public static DirectiveTokenDescriptor CreateToken(DirectiveTokenKind kind)
        {
            return CreateToken(kind, optional: false);
        }

        public static DirectiveTokenDescriptor CreateToken(DirectiveTokenKind kind, bool optional)
        {
            return CreateToken(kind, optional, name: null, description: null);
        }

        public static DirectiveTokenDescriptor CreateToken(DirectiveTokenKind kind, bool optional, string name, string description)
        {
            return new DefaultDirectiveTokenDescriptor(kind, optional, name, description);
        }

        private class DefaultDirectiveTokenDescriptor : DirectiveTokenDescriptor
        {
            public DefaultDirectiveTokenDescriptor(DirectiveTokenKind kind, bool optional, string name, string description)
            {
                Kind = kind;
                Optional = optional;
                Name = name;
                Description = description;
            }

            public override DirectiveTokenKind Kind { get; }

            public override bool Optional { get; }

            public override string Name { get; }

            public override string Description { get; }
        }
    }
}
