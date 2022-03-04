// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers
{
    internal class CharArrayBufferSource : ICharBufferSource
    {
        public static readonly CharArrayBufferSource Instance = new CharArrayBufferSource();

        public char[] Rent(int bufferSize) => new char[bufferSize];

        public void Return(char[] buffer)
        {
            // Do nothing.
        }
    }
}
