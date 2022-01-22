// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class ServicesItem
    {
        public ServicesItem(IInvocationOperation operation)
        {
            Operation = operation;
        }

        public IInvocationOperation Operation { get; }

        public IMethodSymbol UseMethod => Operation.TargetMethod;
    }
}
