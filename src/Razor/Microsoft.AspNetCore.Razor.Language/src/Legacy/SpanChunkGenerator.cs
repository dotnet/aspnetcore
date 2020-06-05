// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal abstract class SpanChunkGenerator : ISpanChunkGenerator
    {
        private static readonly int TypeHashCode = typeof(SpanChunkGenerator).GetHashCode();

        public static readonly ISpanChunkGenerator Null = new NullSpanChunkGenerator();

        public override bool Equals(object obj)
        {
            return obj != null &&
                GetType() == obj.GetType();
        }

        public override int GetHashCode()
        {
            return TypeHashCode;
        }

        private class NullSpanChunkGenerator : ISpanChunkGenerator
        {
            public override string ToString()
            {
                return "None";
            }
        }
    }
}
