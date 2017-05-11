// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class DirectiveTokenDescriptor
    {
        public abstract DirectiveTokenKind Kind { get; }

        public abstract bool Optional { get; }

        public static DirectiveTokenDescriptor CreateToken(DirectiveTokenKind kind)
        {
            return CreateToken(kind, optional: false);
        }

        public static DirectiveTokenDescriptor CreateToken(DirectiveTokenKind kind, bool optional)
        {
            return new DefaultDirectiveTokenDescriptor(kind, optional);
        }

        private class DefaultDirectiveTokenDescriptor : DirectiveTokenDescriptor
        {
            public DefaultDirectiveTokenDescriptor(DirectiveTokenKind kind, bool optional)
            {
                Kind = kind;
                Optional = optional;
            }

            public override DirectiveTokenKind Kind { get; }

            public override bool Optional { get; }
        }
    }
}
