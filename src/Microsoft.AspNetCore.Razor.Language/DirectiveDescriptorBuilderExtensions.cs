// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class DirectiveDescriptorBuilderExtensions
    {
        public static IDirectiveDescriptorBuilder AddMemberToken(this IDirectiveDescriptorBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(DirectiveTokenDescriptor.CreateToken(DirectiveTokenKind.Member));
            return builder;
        }

        public static IDirectiveDescriptorBuilder AddNamespaceToken(this IDirectiveDescriptorBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(DirectiveTokenDescriptor.CreateToken(DirectiveTokenKind.Namespace));
            return builder;
        }

        public static IDirectiveDescriptorBuilder AddStringToken(this IDirectiveDescriptorBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(DirectiveTokenDescriptor.CreateToken(DirectiveTokenKind.String));
            return builder;
        }

        public static IDirectiveDescriptorBuilder AddTypeToken(this IDirectiveDescriptorBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(DirectiveTokenDescriptor.CreateToken(DirectiveTokenKind.Type));
            return builder;
        }

        public static IDirectiveDescriptorBuilder AddOptionalMemberToken(this IDirectiveDescriptorBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(DirectiveTokenDescriptor.CreateToken(DirectiveTokenKind.Member, optional: true));
            return builder;
        }

        public static IDirectiveDescriptorBuilder AddOptionalNamespaceToken(this IDirectiveDescriptorBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(DirectiveTokenDescriptor.CreateToken(DirectiveTokenKind.Namespace, optional: true));
            return builder;
        }

        public static IDirectiveDescriptorBuilder AddOptionalStringToken(this IDirectiveDescriptorBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(DirectiveTokenDescriptor.CreateToken(DirectiveTokenKind.String, optional: true));
            return builder;
        }

        public static IDirectiveDescriptorBuilder AddOptionalTypeToken(this IDirectiveDescriptorBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(DirectiveTokenDescriptor.CreateToken(DirectiveTokenKind.Type, optional: true));
            return builder;
        }
    }
}
