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

using System;
using System.Text;
using Microsoft.AspNet.Razor.Utils;

namespace Microsoft.AspNet.Razor.Test.Framework
{
    public class ErrorCollector
    {
        private StringBuilder _message = new StringBuilder();
        private int _indent = 0;

        public bool Success { get; private set; }

        public string Message
        {
            get { return _message.ToString(); }
        }

        public ErrorCollector()
        {
            Success = true;
        }

        public void AddError(string msg, params object[] args)
        {
            Append("F", msg, args);
            Success = false;
        }

        public void AddMessage(string msg, params object[] args)
        {
            Append("P", msg, args);
        }

        public IDisposable Indent()
        {
            _indent++;
            return new DisposableAction(Unindent);
        }

        public void Unindent()
        {
            _indent--;
        }

        private void Append(string prefix, string msg, object[] args)
        {
            _message.Append(prefix);
            _message.Append(":");
            _message.Append(new String('\t', _indent));
            _message.AppendFormat(msg, args);
            _message.AppendLine();
        }
    }
}
