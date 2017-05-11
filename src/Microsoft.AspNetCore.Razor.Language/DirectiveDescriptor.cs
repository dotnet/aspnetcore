// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class DirectiveDescriptor
    {
        public abstract string Name { get; }

        public abstract DirectiveKind Kind { get; }

        public abstract IReadOnlyList<DirectiveTokenDescriptor> Tokens { get; }

        public static DirectiveDescriptor CreateDirective(string name, DirectiveKind kind)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return CreateDirective(name, kind, configure: null);
        }

        public static DirectiveDescriptor CreateDirective(string name, DirectiveKind kind, Action<IDirectiveDescriptorBuilder> configure)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var builder = new DefaultDirectiveDescriptorBuilder(name, kind);
            configure?.Invoke(builder);
            return builder.Build();
        }

        public static DirectiveDescriptor CreateSingleLineDirective(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return CreateDirective(name, DirectiveKind.SingleLine, configure: null);
        }

        public static DirectiveDescriptor CreateSingleLineDirective(string name, Action<IDirectiveDescriptorBuilder> configure)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return CreateDirective(name, DirectiveKind.SingleLine, configure);
        }

        public static DirectiveDescriptor CreateRazorBlockDirective(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return CreateDirective(name, DirectiveKind.RazorBlock, configure: null);
        }

        public static DirectiveDescriptor CreateRazorBlockDirective(string name, Action<IDirectiveDescriptorBuilder> configure)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return CreateDirective(name, DirectiveKind.RazorBlock, configure);
        }

        public static DirectiveDescriptor CreateCodeBlockDirective(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return CreateDirective(name, DirectiveKind.CodeBlock, configure: null);
        }

        public static DirectiveDescriptor CreateCodeBlockDirective(string name, Action<IDirectiveDescriptorBuilder> configure)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return CreateDirective(name, DirectiveKind.CodeBlock, configure);
        }

        private class DefaultDirectiveDescriptorBuilder : IDirectiveDescriptorBuilder
        {
            public DefaultDirectiveDescriptorBuilder(string name, DirectiveKind kind)
            {
                Name = name;
                Kind = kind;

                Tokens = new List<DirectiveTokenDescriptor>();
            }

            public string Name { get; }

            public DirectiveKind Kind { get; }

            public IList<DirectiveTokenDescriptor> Tokens { get; }

            public DirectiveDescriptor Build()
            {
                if (Name.Length == 0)
                {
                    throw new InvalidOperationException(Resources.FormatDirectiveDescriptor_InvalidDirectiveName(Name));
                }

                for (var i = 0; i < Name.Length; i++)
                {
                    if (!char.IsLetter(Name[i]))
                    {
                        throw new InvalidOperationException(Resources.FormatDirectiveDescriptor_InvalidDirectiveName(Name));
                    }
                }

                var foundOptionalToken = false;
                for (var i = 0; i < Tokens.Count; i++)
                {
                    var token = Tokens[i];
                    foundOptionalToken |= token.Optional;

                    if (foundOptionalToken && !token.Optional)
                    {
                        throw new InvalidOperationException(Resources.DirectiveDescriptor_InvalidNonOptionalToken);
                    }
                }

                return new DefaultDirectiveDescriptor(Name, Kind, Tokens.ToArray());
            }
        }

        private class DefaultDirectiveDescriptor : DirectiveDescriptor
        {
            public DefaultDirectiveDescriptor(string name, DirectiveKind kind, DirectiveTokenDescriptor[] tokens)
            {
                Name = name;
                Kind = kind;
                Tokens = tokens;
            }

            public override string Name { get; }

            public override DirectiveKind Kind { get; }

            public override IReadOnlyList<DirectiveTokenDescriptor> Tokens { get; }
        }
    }
}
