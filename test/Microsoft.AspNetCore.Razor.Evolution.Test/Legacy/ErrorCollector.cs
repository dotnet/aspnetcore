// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class ErrorCollector
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
