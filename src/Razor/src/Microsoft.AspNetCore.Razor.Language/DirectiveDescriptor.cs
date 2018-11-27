// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// A descriptor type for a directive that can be parsed by the Razor parser.
    /// </summary>
    public abstract class DirectiveDescriptor
    {
        /// <summary>
        /// Gets the description of the directive.
        /// </summary>
        /// <remarks>
        /// The description is used for information purposes, and has no effect on parsing.
        /// </remarks>
        public abstract string Description { get; }

        /// <summary>
        /// Gets the directive keyword without the leading <c>@</c> token.
        /// </summary>
        public abstract string Directive { get; }

        /// <summary>
        /// Gets the display name of the directive.
        /// </summary>
        /// <remarks>
        /// The display name is used for information purposes, and has no effect on parsing.
        /// </remarks>
        public abstract string DisplayName { get; }

        /// <summary>
        /// Gets the kind of the directive. The kind determines whether or not a directive has an associated block.
        /// </summary>
        public abstract DirectiveKind Kind { get; }

        /// <summary>
        /// Gets the way a directive can be used. The usage determines how many, and where directives can exist per document.
        /// </summary>
        public abstract DirectiveUsage Usage { get; }

        /// <summary>
        /// Gets the list of directive tokens that can follow the directive keyword.
        /// </summary>
        public abstract IReadOnlyList<DirectiveTokenDescriptor> Tokens { get; }

        /// <summary>
        /// Creates a new <see cref="DirectiveDescriptor"/>.
        /// </summary>
        /// <param name="directive">The directive keyword.</param>
        /// <param name="kind">The directive kind.</param>
        /// <returns>A <see cref="DirectiveDescriptor"/> for the created directive.</returns>
        public static DirectiveDescriptor CreateDirective(string directive, DirectiveKind kind)
        {
            if (directive == null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            return CreateDirective(directive, kind, configure: null);
        }

        /// <summary>
        /// Creates a new <see cref="DirectiveDescriptor"/>.
        /// </summary>
        /// <param name="directive">The directive keyword.</param>
        /// <param name="kind">The directive kind.</param>
        /// <param name="configure">A configuration delegate for the directive.</param>
        /// <returns>A <see cref="DirectiveDescriptor"/> for the created directive.</returns>
        public static DirectiveDescriptor CreateDirective(string directive, DirectiveKind kind, Action<IDirectiveDescriptorBuilder> configure)
        {
            if (directive == null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            var builder = new DefaultDirectiveDescriptorBuilder(directive, kind);
            configure?.Invoke(builder);
            return builder.Build();
        }

        /// <summary>
        /// Creates a new <see cref="DirectiveDescriptor"/> with <see cref="Kind"/> set to <see cref="DirectiveKind.SingleLine"/>
        /// </summary>
        /// <param name="directive">The directive keyword.</param>
        /// <returns>A <see cref="DirectiveDescriptor"/> for the created directive.</returns>
        public static DirectiveDescriptor CreateSingleLineDirective(string directive)
        {
            if (directive == null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            return CreateDirective(directive, DirectiveKind.SingleLine, configure: null);
        }

        /// <summary>
        /// Creates a new <see cref="DirectiveDescriptor"/> with <see cref="Kind"/> set to <see cref="DirectiveKind.SingleLine"/>
        /// </summary>
        /// <param name="directive">The directive keyword.</param>
        /// <param name="configure">A configuration delegate for the directive.</param>
        /// <returns>A <see cref="DirectiveDescriptor"/> for the created directive.</returns>
        public static DirectiveDescriptor CreateSingleLineDirective(string directive, Action<IDirectiveDescriptorBuilder> configure)
        {
            if (directive == null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            return CreateDirective(directive, DirectiveKind.SingleLine, configure);
        }

        /// <summary>
        /// Creates a new <see cref="DirectiveDescriptor"/> with <see cref="Kind"/> set to <see cref="DirectiveKind.RazorBlock"/>
        /// </summary>
        /// <param name="directive">The directive keyword.</param>
        /// <returns>A <see cref="DirectiveDescriptor"/> for the created directive.</returns>
        public static DirectiveDescriptor CreateRazorBlockDirective(string directive)
        {
            if (directive == null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            return CreateDirective(directive, DirectiveKind.RazorBlock, configure: null);
        }

        /// <summary>
        /// Creates a new <see cref="DirectiveDescriptor"/> with <see cref="Kind"/> set to <see cref="DirectiveKind.RazorBlock"/>
        /// </summary>
        /// <param name="directive">The directive keyword.</param>
        /// <param name="configure">A configuration delegate for the directive.</param>
        /// <returns>A <see cref="DirectiveDescriptor"/> for the created directive.</returns>
        public static DirectiveDescriptor CreateRazorBlockDirective(string directive, Action<IDirectiveDescriptorBuilder> configure)
        {
            if (directive == null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            return CreateDirective(directive, DirectiveKind.RazorBlock, configure);
        }

        /// <summary>
        /// Creates a new <see cref="DirectiveDescriptor"/> with <see cref="Kind"/> set to <see cref="DirectiveKind.CodeBlock"/>
        /// </summary>
        /// <param name="directive">The directive keyword.</param>
        /// <returns>A <see cref="DirectiveDescriptor"/> for the created directive.</returns>
        public static DirectiveDescriptor CreateCodeBlockDirective(string directive)
        {
            if (directive == null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            return CreateDirective(directive, DirectiveKind.CodeBlock, configure: null);
        }

        /// <summary>
        /// Creates a new <see cref="DirectiveDescriptor"/> with <see cref="Kind"/> set to <see cref="DirectiveKind.CodeBlock"/>
        /// </summary>
        /// <param name="directive">The directive keyword.</param>
        /// <param name="configure">A configuration delegate for the directive.</param>
        /// <returns>A <see cref="DirectiveDescriptor"/> for the created directive.</returns>
        public static DirectiveDescriptor CreateCodeBlockDirective(string directive, Action<IDirectiveDescriptorBuilder> configure)
        {
            if (directive == null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            return CreateDirective(directive, DirectiveKind.CodeBlock, configure);
        }

        private class DefaultDirectiveDescriptorBuilder : IDirectiveDescriptorBuilder
        {
            public DefaultDirectiveDescriptorBuilder(string name, DirectiveKind kind)
            {
                Directive = name;
                Kind = kind;

                Tokens = new List<DirectiveTokenDescriptor>();
            }

            public string Description { get; set; }

            public string Directive { get; }

            public string DisplayName { get; set; }

            public DirectiveKind Kind { get; }

            public DirectiveUsage Usage { get; set; }

            public IList<DirectiveTokenDescriptor> Tokens { get; }

            public DirectiveDescriptor Build()
            {
                if (Directive.Length == 0)
                {
                    throw new InvalidOperationException(Resources.FormatDirectiveDescriptor_InvalidDirectiveKeyword(Directive));
                }

                for (var i = 0; i < Directive.Length; i++)
                {
                    if (!char.IsLetter(Directive[i]))
                    {
                        throw new InvalidOperationException(Resources.FormatDirectiveDescriptor_InvalidDirectiveKeyword(Directive));
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

                return new DefaultDirectiveDescriptor(Directive, Kind, Usage, Tokens.ToArray(), DisplayName, Description);
            }
        }

        private class DefaultDirectiveDescriptor : DirectiveDescriptor
        {
            public DefaultDirectiveDescriptor(
                string directive, 
                DirectiveKind kind, 
                DirectiveUsage usage,
                DirectiveTokenDescriptor[] tokens,
                string displayName,
                string description)
            {
                Directive = directive;
                Kind = kind;
                Usage = usage;
                Tokens = tokens;
                DisplayName = displayName;
                Description = description;
            }

            public override string Description { get; }

            public override string Directive { get; }

            public override string DisplayName { get; }

            public override DirectiveKind Kind { get; }

            public override DirectiveUsage Usage { get; }

            public override IReadOnlyList<DirectiveTokenDescriptor> Tokens { get; }
        }
    }
}
