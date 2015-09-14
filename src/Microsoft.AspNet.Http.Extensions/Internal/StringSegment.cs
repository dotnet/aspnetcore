using System;

namespace Microsoft.AspNet.Http.Internal
{
    internal struct StringSegment : IEquatable<StringSegment>
    {
        private readonly string _buffer;
        private readonly int _offset;
        private readonly int _count;

        // <summary>
        // Initializes a new instance of the <see cref="T:System.Object"/> class.
        // </summary>
        public StringSegment(string buffer, int offset, int count)
        {
            _buffer = buffer;
            _offset = offset;
            _count = count;
        }

        public string Buffer
        {
            get { return _buffer; }
        }

        public int Offset
        {
            get { return _offset; }
        }

        public int Count
        {
            get { return _count; }
        }

        public string Value
        {
            get { return _offset == -1 ? null : _buffer.Substring(_offset, _count); }
        }

        public bool HasValue
        {
            get { return _offset != -1 && _count != 0 && _buffer != null; }
        }

        public bool Equals(StringSegment other)
        {
            return string.Equals(_buffer, other._buffer) && _offset == other._offset && _count == other._count;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is StringSegment && Equals((StringSegment)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (_buffer != null ? _buffer.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ _offset;
                hashCode = (hashCode * 397) ^ _count;
                return hashCode;
            }
        }

        public static bool operator ==(StringSegment left, StringSegment right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StringSegment left, StringSegment right)
        {
            return !left.Equals(right);
        }

        public bool StartsWith(string text, StringComparison comparisonType)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            int textLength = text.Length;
            if (!HasValue || _count < textLength)
            {
                return false;
            }

            return string.Compare(_buffer, _offset, text, 0, textLength, comparisonType) == 0;
        }

        public bool EndsWith(string text, StringComparison comparisonType)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            int textLength = text.Length;
            if (!HasValue || _count < textLength)
            {
                return false;
            }

            return string.Compare(_buffer, _offset + _count - textLength, text, 0, textLength, comparisonType) == 0;
        }

        public bool Equals(string text, StringComparison comparisonType)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            int textLength = text.Length;
            if (!HasValue || _count != textLength)
            {
                return false;
            }

            return string.Compare(_buffer, _offset, text, 0, textLength, comparisonType) == 0;
        }

        public string Substring(int offset, int length)
        {
            return _buffer.Substring(_offset + offset, length);
        }

        public StringSegment Subsegment(int offset, int length)
        {
            return new StringSegment(_buffer, _offset + offset, length);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }

}
