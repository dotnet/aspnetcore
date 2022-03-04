// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite
{
    /// <summary>
    /// Represents a string iterator, with captures.
    /// </summary>
    internal class ParserContext
    {
        public readonly string Template;
        public int Index { get; set; }
        private int? _mark;

        public ParserContext(string condition)
        {
            Template = condition;
            Index = -1;
        }

        public char Current => (Index < Template.Length && Index >= 0) ? Template[Index] : (char)0;

        public bool Back()
        {
            return --Index >= 0;
        }

        public bool Next()
        {
            return ++Index < Template.Length;
        }

        public bool HasNext()
        {
            return (Index + 1) < Template.Length;
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
                var value = Template.Substring(_mark.Value, Index - _mark.Value);
                _mark = null;
                return value;
            }
            else
            {
                return null;
            }
        }
    }
}
