// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Runtime.Precompilation
{
    [MultipleConstructorArguments(firstArgument: "First1", secondArgument: "Second1", thirdArgument: 31)]
    [MultipleConstructorArguments(secondArgument: "Second2", firstArgument: "First2", thirdArgument: 32)]
    [MultipleConstructorArguments(thirdArgument: 33, secondArgument: "Second3", firstArgument: "First3")]
    public class TypeWithNamedAttributes
    {
    }
}
