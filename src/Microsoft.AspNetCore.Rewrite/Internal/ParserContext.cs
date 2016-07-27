// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.Internal
{
    /// <summary>
    /// Represents a string iterator, with captures.
    /// </summary>
    public class ParserContext
    {
        private readonly string _template;
        private int _index;
        private int? _mark;

        public ParserContext(string condition)
        {
            _template = condition;
            _index = -1;
        }

        public char Current
        {
            get { return (_index < _template.Length && _index >= 0) ? _template[_index] : (char)0; }
        }

        public bool Back()
        {
            return --_index >= 0;
        }

        public bool Next()
        {
            return ++_index < _template.Length;
        }

        public bool HasNext()
        {
            return (_index + 1) < _template.Length;
        }

        public void Mark()
        {
            _mark = _index;
        }

        public int GetIndex()
        {
            return _index;
        }

        public string Capture()
        {
            // TODO make this return a range rather than a string.
            if (_mark.HasValue)
            {
                var value = _template.Substring(_mark.Value, _index - _mark.Value);
                _mark = null;
                return value;
            }
            else
            {
                return null;
            }
        }
        public string Error()
        {
            return string.Format("Syntax Error at index: ", _index, " with character: ", Current); 
        }
    }
}
