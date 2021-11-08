// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

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
