using System;
using System.Text;
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class LegacySourceDocument : RazorSourceDocument
    {
        private readonly ITextBuffer _buffer;
        private readonly string _filename;
        private readonly RazorSourceLineCollection _lines;

        public static RazorSourceDocument Create(ITextBuffer buffer, string filename)
        {
            return new LegacySourceDocument(buffer, filename); 
        }

        private LegacySourceDocument(ITextBuffer buffer, string filename)
        {
            _buffer = buffer;
            _filename = filename;

            _lines = new DefaultRazorSourceLineCollection(this);
        }

        public override char this[int position]
        {
            get
            {
                _buffer.Position = position;
                return (char)_buffer.Read();
            }
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override string FileName => _filename;

        public override int Length => _buffer.Length;

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

            for (var i = 0; i < count; i++)
            {
                destination[destinationIndex + i] = this[sourceIndex + i];
            }
        }
    }
}
