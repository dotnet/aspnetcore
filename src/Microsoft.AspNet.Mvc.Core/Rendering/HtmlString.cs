// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class HtmlString
    {
        private static readonly HtmlString _empty = new HtmlString(string.Empty);

        private readonly StringCollectionTextWriter _writer;
        private readonly string _input;

        public HtmlString(string input)
        {
            _input = input;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="HtmlString"/> that is backed by <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer"></param>
        public HtmlString([NotNull] StringCollectionTextWriter writer)
        {
            _writer = writer;
        }

        public static HtmlString Empty
        {
            get
            {
                return _empty;
            }
        }

        /// <summary>
        /// Writes the value in this instance of <see cref="HtmlString"/> to the target <paramref name="targetWriter"/>.
        /// </summary>
        /// <param name="targetWriter">The <see cref="TextWriter"/> to write contents to.</param>
        public void WriteTo(TextWriter targetWriter)
        {
            if (_writer != null)
            {
                _writer.CopyTo(targetWriter);
            }
            else
            {
                targetWriter.Write(_input);
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (_writer != null)
            {
                return _writer.ToString();
            }

            return _input;
        }
    }
}
