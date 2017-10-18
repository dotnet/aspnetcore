// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Dispatcher.Patterns
{
    /// <summary>
    /// The parsed representation of a constraint in a <see cref="RoutePattern"/> parameter.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString()}")]
    public sealed class ConstraintReference
    {
        /// <summary>
        /// Creates a new <see cref="ConstraintReference"/>.
        /// </summary>
        /// <param name="content">The constraint identifier.</param>
        /// <remarks>A new <see cref="ConstraintReference"/>.</remarks>
        public static ConstraintReference Create(string content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            return new ConstraintReference(null, content);
        }

        /// <summary>
        /// Creates a new <see cref="ConstraintReference"/>.
        /// </summary>
        /// <param name="rawText">The raw text of the constraint identifier.</param>
        /// <param name="content">The constraint identifier.</param>
        /// <remarks>A new <see cref="ConstraintReference"/>.</remarks>
        public static ConstraintReference CreateFromText(string rawText, string content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            return new ConstraintReference(rawText, content);
        }

        private ConstraintReference(string rawText, string content)
        {
            RawText = rawText;
            Content = content;
        }

        /// <summary>
        /// Gets the constraint text.
        /// </summary>
        public string Content { get; }

        public string RawText { get; }

        private string DebuggerToString()
        {
            return RawText ?? Content;
        }
    }
}