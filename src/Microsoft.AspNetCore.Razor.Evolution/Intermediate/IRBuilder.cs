// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public abstract class IRBuilder
    {
        public static IRBuilder Document()
        {
            var builder = new DefaultIRBuilder();
            builder.Push(new IRDocument());
            return builder;
        }

        public abstract IRNode Current { get; }

        public abstract void Add(IRNode node);

        public abstract void Push(IRNode node);

        public abstract IRNode Pop();
    }
}
