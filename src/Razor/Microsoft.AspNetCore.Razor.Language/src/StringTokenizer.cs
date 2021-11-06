// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal readonly struct StringTokenizer
{
    private readonly StringSegment _value;
    private readonly char[] _separators;

    /// <summary>
    /// Initializes a new instance of <see cref="StringTokenizer"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to tokenize.</param>
    /// <param name="separators">The characters to tokenize by.</param>
    public StringTokenizer(string value, char[] separators)
    {
        _value = value;
        _separators = separators;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="StringTokenizer"/>.
    /// </summary>
    /// <param name="value">The <see cref="StringSegment"/> to tokenize.</param>
    /// <param name="separators">The characters to tokenize by.</param>
    public StringTokenizer(StringSegment value, char[] separators)
    {
        _value = value;
        _separators = separators;
    }

    public Enumerator GetEnumerator() => new Enumerator(in _value, _separators);

    public struct Enumerator
    {
        private readonly StringSegment _value;
        private readonly char[] _separators;
        private int _index;

        internal Enumerator(in StringSegment value, char[] separators)
        {
            _value = value;
            _separators = separators;
            Current = default;
            _index = 0;
        }

        public Enumerator(ref StringTokenizer tokenizer)
        {
            _value = tokenizer._value;
            _separators = tokenizer._separators;
            Current = default(StringSegment);
            _index = 0;
        }

        public StringSegment Current { get; private set; }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (!_value.HasValue || _index > _value.Length)
            {
                Current = default(StringSegment);
                return false;
            }

            int next = _value.IndexOfAny(_separators, _index);
            if (next == -1)
            {
                // No separator found. Consume the remainder of the string.
                next = _value.Length;
            }

            Current = _value.Subsegment(_index, next - _index);
            _index = next + 1;

            return true;
        }
    }
}
