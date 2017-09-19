// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class TextSnapshotSourceDocument : RazorSourceDocument
    {
        private readonly ITextSnapshot _buffer;
        private readonly RazorSourceLineCollection _lines;

        public TextSnapshotSourceDocument(ITextSnapshot snapshot, string filePath)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            _buffer = snapshot;
            FilePath = filePath;

            _lines = new DefaultRazorSourceLineCollection(this);
        }

        public override char this[int position] => _buffer[position];

        public override Encoding Encoding => Encoding.UTF8;

        public override int Length => _buffer.Length;

        public override RazorSourceLineCollection Lines => _lines;

        public override string FilePath { get; }

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

        public override byte[] GetChecksum() => throw new NotImplementedException();
    }
}