// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class DirectiveDescriptorBuilderExtensions
    {
        public static IDirectiveDescriptorBuilder AddMemberToken(this IDirectiveDescriptorBuilder builder)
        {
            return AddMemberToken(builder, name: null, description: null);
        }

        public static IDirectiveDescriptorBuilder AddMemberToken(this IDirectiveDescriptorBuilder builder, string name, string description)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(
                DirectiveTokenDescriptor.CreateToken(
                    DirectiveTokenKind.Member,
                    optional: false,
                    name: name,
                    description: description));

            return builder;
        }

        public static IDirectiveDescriptorBuilder AddNamespaceToken(this IDirectiveDescriptorBuilder builder)
        {
            return AddNamespaceToken(builder, name: null, description: null);
        }

        public static IDirectiveDescriptorBuilder AddNamespaceToken(this IDirectiveDescriptorBuilder builder, string name, string description)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(
                DirectiveTokenDescriptor.CreateToken(
                    DirectiveTokenKind.Namespace,
                    optional: false,
                    name: name,
                    description: description));

            return builder;
        }

        public static IDirectiveDescriptorBuilder AddStringToken(this IDirectiveDescriptorBuilder builder)
        {
            return AddStringToken(builder, name: null, description: null);
        }

        public static IDirectiveDescriptorBuilder AddStringToken(this IDirectiveDescriptorBuilder builder, string name, string description)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(
                DirectiveTokenDescriptor.CreateToken(
                    DirectiveTokenKind.String,
                    optional: false,
                    name: name,
                    description: description));

            return builder;
        }

        public static IDirectiveDescriptorBuilder AddTypeToken(this IDirectiveDescriptorBuilder builder)
        {
            return AddTypeToken(builder, name: null, description: null);
        }

        public static IDirectiveDescriptorBuilder AddTypeToken(this IDirectiveDescriptorBuilder builder, string name, string description)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(
                DirectiveTokenDescriptor.CreateToken(
                    DirectiveTokenKind.Type,
                    optional: false,
                    name: name,
                    description: description));

            return builder;
        }

        public static IDirectiveDescriptorBuilder AddAttributeToken(this IDirectiveDescriptorBuilder builder)
        {
            return AddAttributeToken(builder, name: null, description: null);
        }

        public static IDirectiveDescriptorBuilder AddAttributeToken(this IDirectiveDescriptorBuilder builder, string name, string description)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(
                DirectiveTokenDescriptor.CreateToken(
                    DirectiveTokenKind.Attribute,
                    optional: false,
                    name: name,
                    description: description));

            return builder;
        }

        public static IDirectiveDescriptorBuilder AddBooleanToken(this IDirectiveDescriptorBuilder builder)
        {
            return AddBooleanToken(builder, name: null, description: null);
        }

        public static IDirectiveDescriptorBuilder AddBooleanToken(this IDirectiveDescriptorBuilder builder, string name, string description)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(
                DirectiveTokenDescriptor.CreateToken(
                    DirectiveTokenKind.Boolean,
                    optional: false,
                    name: name,
                    description: description));

            return builder;
        }

        public static IDirectiveDescriptorBuilder AddOptionalMemberToken(this IDirectiveDescriptorBuilder builder)
        {
            return AddOptionalMemberToken(builder, name: null, description: null);
        }

        public static IDirectiveDescriptorBuilder AddOptionalMemberToken(this IDirectiveDescriptorBuilder builder, string name, string description)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(
                DirectiveTokenDescriptor.CreateToken(
                    DirectiveTokenKind.Member,
                    optional: true,
                    name: name,
                    description: description));

            return builder;
        }

        public static IDirectiveDescriptorBuilder AddOptionalNamespaceToken(this IDirectiveDescriptorBuilder builder)
        {
            return AddOptionalNamespaceToken(builder, name: null, description: null);
        }

        public static IDirectiveDescriptorBuilder AddOptionalNamespaceToken(this IDirectiveDescriptorBuilder builder, string name, string description)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(
                DirectiveTokenDescriptor.CreateToken(
                    DirectiveTokenKind.Namespace,
                    optional: true,
                    name: name,
                    description: description));

            return builder;
        }

        public static IDirectiveDescriptorBuilder AddOptionalStringToken(this IDirectiveDescriptorBuilder builder)
        {
            return AddOptionalStringToken(builder, name: null, description: null);
        }

        public static IDirectiveDescriptorBuilder AddOptionalStringToken(this IDirectiveDescriptorBuilder builder, string name, string description)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(
                DirectiveTokenDescriptor.CreateToken(
                    DirectiveTokenKind.String,
                    optional: true,
                    name: name,
                    description: description));

            return builder;
        }

        public static IDirectiveDescriptorBuilder AddOptionalTypeToken(this IDirectiveDescriptorBuilder builder)
        {
            return AddOptionalTypeToken(builder, name: null, description: null);
        }

        public static IDirectiveDescriptorBuilder AddOptionalTypeToken(this IDirectiveDescriptorBuilder builder, string name, string description)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(
                DirectiveTokenDescriptor.CreateToken(
                    DirectiveTokenKind.Type,
                    optional: true,
                    name: name,
                    description: description));

            return builder;
        }

        public static IDirectiveDescriptorBuilder AddOptionalAttributeToken(this IDirectiveDescriptorBuilder builder)
        {
            return AddOptionalAttributeToken(builder, name: null, description: null);
        }

        public static IDirectiveDescriptorBuilder AddOptionalAttributeToken(this IDirectiveDescriptorBuilder builder, string name, string description)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Tokens.Add(
                DirectiveTokenDescriptor.CreateToken(
                    DirectiveTokenKind.Attribute,
                    optional: true,
                    name: name,
                    description: description));

            return builder;
        }
    }
}
