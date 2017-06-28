// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorParserOptions
    {
        public static RazorParserOptions Create(IEnumerable<DirectiveDescriptor> directives, bool designTime)
        {
            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            return new DefaultRazorParserOptions(directives.ToArray(), designTime, parseOnlyLeadingDirectives: false);
        }

        public static RazorParserOptions Create(IEnumerable<DirectiveDescriptor> directives, bool designTime, bool parseOnlyLeadingDirectives)
        {
            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            return new DefaultRazorParserOptions(directives.ToArray(), designTime, parseOnlyLeadingDirectives);
        }

        public static RazorParserOptions CreateDefault()
        {
            return new DefaultRazorParserOptions(Array.Empty<DirectiveDescriptor>(), designTime: false, parseOnlyLeadingDirectives: false);
        }

        public abstract bool DesignTime { get; }

        public abstract IReadOnlyCollection<DirectiveDescriptor> Directives { get; }

        /// <summary>
        /// Gets a value which indicates whether the parser will parse only the leading directives. If <c>true</c>
        /// the parser will halt at the first HTML content or C# code block. If <c>false</c> the whole document is parsed.
        /// </summary>
        /// <remarks>
        /// Currently setting this option to <c>true</c> will result in only the first line of directives being parsed.
        /// In a future release this may be updated to include all leading directive content.
        /// </remarks>
        public abstract bool ParseOnlyLeadingDirectives { get; }
    }
}
