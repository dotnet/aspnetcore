// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.Html.Abstractions;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// String content which knows how to write itself.
    /// </summary>
    public class HtmlString : IHtmlContent
    {
        private static readonly HtmlString _empty = new HtmlString(string.Empty);
        private readonly string _input;

        /// <summary>
        /// Instantiates a new instance of <see cref="HtmlString"/>.
        /// </summary>
        /// <param name="input"><c>string</c>to initialize <see cref="HtmlString"/>.</param>
        public HtmlString(string input)
        {
            _input = input;
        }

        /// <summary>
        /// Returns an <see cref="HtmlString"/> with empty content.
        /// </summary>
        public static HtmlString Empty
        {
            get
            {
                return _empty;
            }
        }

        /// <summary>
        /// Writes the value in this instance of <see cref="HtmlString"/> to the target
        /// <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to write contents to.</param>
        /// <param name="encoder">The <see cref="IHtmlEncoder"/> with which the output must be encoded.</param>
        public void WriteTo(TextWriter writer, IHtmlEncoder encoder)
        {
            writer.Write(_input);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return _input;
        }
    }
}
