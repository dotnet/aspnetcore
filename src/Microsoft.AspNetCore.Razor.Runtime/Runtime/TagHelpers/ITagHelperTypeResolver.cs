// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Locates valid <see cref="ITagHelper"/>s within an assembly.
    /// </summary>
    public interface ITagHelperTypeResolver
    {
        /// <summary>
        /// Locates valid <see cref="ITagHelper"/> types from the <see cref="Assembly"/> named <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of an <see cref="Assembly"/> to search.</param>
        /// <param name="documentLocation">The <see cref="SourceLocation"/> of the associated
        /// <see cref="Parser.SyntaxTree.SyntaxTreeNode"/> responsible for the current <see cref="Resolve"/> call.
        /// </param>
        /// <param name="errorSink">The <see cref="ErrorSink"/> used to record errors found when resolving
        /// <see cref="ITagHelper"/> types.</param>
        /// <returns>An <see cref="IEnumerable{Type}"/> of valid <see cref="ITagHelper"/> types.</returns>
        IEnumerable<Type> Resolve(string name, SourceLocation documentLocation, ErrorSink errorSink);
    }
}
