// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.AspNetCore.Testing
{
    public class RepeatContext
    {
        private static AsyncLocal<RepeatContext> _current = new AsyncLocal<RepeatContext>();

        public static RepeatContext Current
        {
            get => _current.Value;
            internal set => _current.Value = value;
        }

        public RepeatContext(int limit)
        {
            Limit = limit;
        }

        public int Limit { get; }

        public int CurrentIteration { get; set; }
    }
}
