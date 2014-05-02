// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using Microsoft.AspNet.Razor.Text;

namespace System.Web.WebPages.TestUtils
{
    public class StringTextBuffer : ITextBuffer, IDisposable
    {
        private string _buffer;
        public bool Disposed { get; set; }

        public StringTextBuffer(string buffer)
        {
            _buffer = buffer;
        }

        public int Length
        {
            get { return _buffer.Length; }
        }

        public int Position { get; set; }

        public int Read()
        {
            if (Position >= _buffer.Length)
            {
                return -1;
            }
            return _buffer[Position++];
        }

        public int Peek()
        {
            if (Position >= _buffer.Length)
            {
                return -1;
            }
            return _buffer[Position];
        }

        public void Dispose()
        {
            Disposed = true;
        }

        public object VersionToken
        {
            get { return _buffer; }
        }
    }
}
