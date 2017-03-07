// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultRazorSourceDocument : RazorSourceDocument
    {
        private readonly string _content;
        private readonly RazorSourceLineCollection _lines;

        public DefaultRazorSourceDocument(string content, Encoding encoding, string filename)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            _content = content;
            Encoding = encoding;
            Filename = filename;

            _lines = new DefaultRazorSourceLineCollection(this);
        }

        public override char this[int position] => _content[position];

        public override Encoding Encoding { get; }

        public override string Filename { get; }

        public override int Length => _content.Length;

        public override RazorSourceLineCollection Lines => _lines;

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (sourceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            }

            if (destinationIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationIndex));
            }

            if (count < 0 || count > Length - sourceIndex || count > destination.Length - destinationIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (count == 0)
            {
                return;
            }

            _content.CopyTo(sourceIndex, destination, destinationIndex, count);
        }
    }
}
