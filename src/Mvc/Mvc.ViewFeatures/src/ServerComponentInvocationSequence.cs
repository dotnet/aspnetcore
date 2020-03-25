// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    // Used to produce a monotonically increasing sequence starting at 0 that is unique for the scope of the top-level page/view/component being rendered.
    internal class ServerComponentInvocationSequence
    {
        private int _sequence;

        public ServerComponentInvocationSequence()
        {
            Span<byte> bytes = stackalloc byte[16];
            RandomNumberGenerator.Fill(bytes);
            Value = new Guid(bytes);
            _sequence = -1;
        }

        public Guid Value { get; }

        public int Next() => ++_sequence;
    }
}
