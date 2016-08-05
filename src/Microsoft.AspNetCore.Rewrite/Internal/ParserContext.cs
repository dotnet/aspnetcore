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
        public int Index { get; set; }
        private int? _mark;

        public ParserContext(string condition)
        {
            _template = condition;
            Index = -1;
        }

        public char Current
        {
            get { return (Index < _template.Length && Index >= 0) ? _template[Index] : (char)0; }
        }

        public bool Back()
        {
            return --Index >= 0;
        }

        public bool Next()
        {
            return ++Index < _template.Length;
        }

        public bool HasNext()
        {
            return (Index + 1) < _template.Length;
        }

        public void Mark()
        {
            _mark = Index;
        }

        public int GetIndex()
        {
            return Index;
        }

        public string Capture()
        {
            // TODO make this return a range rather than a string.
            if (_mark.HasValue)
            {
                var value = _template.Substring(_mark.Value, Index - _mark.Value);
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
            return string.Format("Syntax Error at index: ", Index, " with character: ", Current); 
        }
    }
}
