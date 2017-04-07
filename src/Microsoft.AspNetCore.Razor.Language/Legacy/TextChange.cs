// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public struct TextChange
    {
        private string _newText;
        private string _oldText;

        /// <summary>
        /// Constructor for changes where the position hasn't moved (primarily for tests)
        /// </summary>
        internal TextChange(int position, int oldLength, ITextBuffer oldBuffer, int newLength, ITextBuffer newBuffer)
            : this(position, oldLength, oldBuffer, position, newLength, newBuffer)
        {
        }

        public TextChange(
            int oldPosition,
            int oldLength,
            ITextBuffer oldBuffer,
            int newPosition,
            int newLength,
            ITextBuffer newBuffer)
            : this()
        {
            if (oldBuffer == null)
            {
                throw new ArgumentNullException(nameof(oldBuffer));
            }

            if (newBuffer == null)
            {
                throw new ArgumentNullException(nameof(newBuffer));
            }

            if (oldPosition < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(oldPosition), LegacyResources.FormatArgument_Must_Be_GreaterThanOrEqualTo(0));
            }
            if (newPosition < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newPosition), LegacyResources.FormatArgument_Must_Be_GreaterThanOrEqualTo(0));
            }
            if (oldLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(oldLength), LegacyResources.FormatArgument_Must_Be_GreaterThanOrEqualTo(0));
            }
            if (newLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newLength), LegacyResources.FormatArgument_Must_Be_GreaterThanOrEqualTo(0));
            }

            OldPosition = oldPosition;
            NewPosition = newPosition;
            OldLength = oldLength;
            NewLength = newLength;
            NewBuffer = newBuffer;
            OldBuffer = oldBuffer;
        }

        public int OldPosition { get; }

        public int NewPosition { get; }

        public int OldLength { get; }

        public int NewLength { get; }

        public ITextBuffer NewBuffer { get; }

        public ITextBuffer OldBuffer { get; }

        /// <remark>
        /// Note: This property is not thread safe, and will move position on the textbuffer while being read.
        /// https://aspnetwebstack.codeplex.com/workitem/1317, tracks making this immutable and improving the access
        /// to ITextBuffer to be thread safe.
        /// </remark>
        public string OldText
        {
            get
            {
                if (_oldText == null && OldBuffer != null)
                {
                    _oldText = GetText(OldBuffer, OldPosition, OldLength);
                }
                return _oldText;
            }
        }

        /// <remark>
        /// Note: This property is not thread safe, and will move position on the textbuffer while being read.
        /// https://aspnetwebstack.codeplex.com/workitem/1317, tracks making this immutable and improving the access
        /// to ITextBuffer to be thread safe.
        /// </remark>
        public string NewText
        {
            get
            {
                if (_newText == null)
                {
                    _newText = GetText(NewBuffer, NewPosition, NewLength);
                }
                return _newText;
            }
        }

        public bool IsInsert
        {
            get { return OldLength == 0 && NewLength > 0; }
        }

        public bool IsDelete
        {
            get { return OldLength > 0 && NewLength == 0; }
        }

        public bool IsReplace
        {
            get { return OldLength > 0 && NewLength > 0; }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TextChange))
            {
                return false;
            }

            var change = (TextChange)obj;
            return change.OldPosition == OldPosition &&
                change.NewPosition == NewPosition &&
                change.OldLength == OldLength &&
                change.NewLength == NewLength &&
                OldBuffer.Equals(change.OldBuffer) &&
                NewBuffer.Equals(change.NewBuffer);
        }

        public override int GetHashCode()
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(OldPosition);
            hashCodeCombiner.Add(NewPosition);
            hashCodeCombiner.Add(OldLength);
            hashCodeCombiner.Add(NewLength);
            hashCodeCombiner.Add(OldBuffer);
            hashCodeCombiner.Add(NewBuffer);

            return hashCodeCombiner;
        }

        public string ApplyChange(string content, int changeOffset)
        {
            var changeRelativePosition = OldPosition - changeOffset;

            Debug.Assert(changeRelativePosition >= 0);
            return content.Remove(changeRelativePosition, OldLength)
                .Insert(changeRelativePosition, NewText);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "({0}:{1}) \"{3}\" -> ({0}:{2}) \"{4}\"", OldPosition, OldLength, NewLength, OldText, NewText);
        }

        /// <summary>
        /// Removes a common prefix from the edit to turn IntelliSense replacements into insertions where possible
        /// </summary>
        /// <returns>A normalized text change</returns>
        public TextChange Normalize()
        {
            if (OldBuffer != null && IsReplace && NewLength > OldLength && NewText.StartsWith(OldText, StringComparison.Ordinal) && NewPosition == OldPosition)
            {
                // Normalize the change into an insertion of the uncommon suffix (i.e. strip out the common prefix)
                return new TextChange(oldPosition: OldPosition + OldLength,
                                      oldLength: 0,
                                      oldBuffer: OldBuffer,
                                      newPosition: OldPosition + OldLength,
                                      newLength: NewLength - OldLength,
                                      newBuffer: NewBuffer);
            }
            return this;
        }

        private static string GetText(ITextBuffer buffer, int position, int length)
        {
            // Optimization for the common case of one char inserts, in this case we don't even need to seek the buffer.
            if (length == 0)
            {
                return string.Empty;
            }

            var oldPosition = buffer.Position;
            try
            {
                buffer.Position = position;

                // Optimization for the common case of one char inserts, in this case we seek the buffer.
                if (length == 1)
                {
                    return ((char)buffer.Read()).ToString();
                }
                else
                {
                    var builder = new StringBuilder();
                    for (int i = 0; i < length; i++)
                    {
                        var c = (char)buffer.Read();
                        builder.Append(c);

                        // This check is probably not necessary, will revisit when fixing https://aspnetwebstack.codeplex.com/workitem/1317
                        if (Char.IsHighSurrogate(c))
                        {
                            builder.Append((char)buffer.Read());
                        }
                    }
                    return builder.ToString();
                }
            }
            finally
            {
                buffer.Position = oldPosition;
            }
        }

        public static bool operator ==(TextChange left, TextChange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextChange left, TextChange right)
        {
            return !left.Equals(right);
        }
    }
}
