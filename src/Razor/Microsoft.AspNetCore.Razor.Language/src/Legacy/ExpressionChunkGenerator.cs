// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class ExpressionChunkGenerator : ISpanChunkGenerator
    {
        private static readonly int TypeHashCode = typeof(ExpressionChunkGenerator).GetHashCode();

        public override string ToString()
        {
            return "Expr";
        }

        public override bool Equals(object obj)
        {
            return obj != null &&
                GetType() == obj.GetType();
        }

        public override int GetHashCode()
        {
            return TypeHashCode;
        }
    }
}
