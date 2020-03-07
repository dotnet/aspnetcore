// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class OptionsItem
    {
        public OptionsItem(IPropertySymbol property, object constantValue)
        {
            Property = property;
            ConstantValue = constantValue;
        }

        public INamedTypeSymbol OptionsType => Property.ContainingType;

        public IPropertySymbol Property { get; }

        public object ConstantValue { get; }
    }
}
