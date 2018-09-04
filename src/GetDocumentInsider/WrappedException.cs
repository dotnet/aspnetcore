// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace GetDocument
{
    internal class WrappedException : Exception
    {
        private readonly string _stackTrace;

        public WrappedException(string type, string message, string stackTrace)
            : base(message)
        {
            Type = type;
            _stackTrace = stackTrace;
        }

        public string Type { get; }

        public override string ToString()
            => _stackTrace;
    }
}
