// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// A builder interface for configuring a <see cref="DirectiveDescriptor"/>.
    /// </summary>
    public interface IDirectiveDescriptorBuilder
    {
        /// <summary>
        /// Gets or sets the description of the directive.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Gets the directive keyword.
        /// </summary>
        string Directive { get; }

        /// <summary>
        /// Gets or sets the display name of the directive.
        /// </summary>
        string DisplayName { get; set; }

        /// <summary>
        /// Gets the directive kind.
        /// </summary>
        DirectiveKind Kind { get; }

        /// <summary>
        /// Gets or sets the directive usage. The usage determines how many, and where directives can exist per document.
        /// </summary>
        DirectiveUsage Usage { get; set; }

        /// <summary>
        /// Gets a list of the directive tokens.
        /// </summary>
        IList<DirectiveTokenDescriptor> Tokens { get; }

        /// <summary>
        /// Creates a <see cref="DirectiveDescriptor"/> based on the current property values of the builder.
        /// </summary>
        /// <returns>The created <see cref="DirectiveDescriptor" />.</returns>
        DirectiveDescriptor Build();
    }
}
