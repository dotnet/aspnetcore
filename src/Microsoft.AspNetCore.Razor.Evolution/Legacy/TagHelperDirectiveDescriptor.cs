// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    /// <summary>
    /// Contains information needed to resolve <see cref="TagHelperDescriptor"/>s.
    /// </summary>
    internal class TagHelperDirectiveDescriptor
    {
        private string _directiveText;

        /// <summary>
        /// A <see cref="string"/> used to find tag helper <see cref="System.Type"/>s.
        /// </summary>
        public string DirectiveText
        {
            get
            {
                return _directiveText;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _directiveText = value;
            }
        }

        /// <summary>
        /// The <see cref="SourceLocation"/> of the directive.
        /// </summary>
        public SourceLocation Location { get; set; } = SourceLocation.Zero;

        /// <summary>
        /// The <see cref="TagHelperDirectiveType"/> of this directive.
        /// </summary>
        public TagHelperDirectiveType DirectiveType { get; set; }
    }
}