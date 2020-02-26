// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public interface IsAssignable_ReturnsTrueIfAncestorTypeImplementsInterface
    {
    }

    public class IsAssignable_ReturnsTrueIfAncestorTypeImplementsInterfaceA : IsAssignable_ReturnsTrueIfAncestorTypeImplementsInterface
    {
    }

    public class IsAssignable_ReturnsTrueIfAncestorTypeImplementsInterfaceB : IsAssignable_ReturnsTrueIfAncestorTypeImplementsInterfaceA
    {
    }

    public class IsAssignable_ReturnsTrueIfAncestorTypeImplementsInterfaceTest : IsAssignable_ReturnsTrueIfAncestorTypeImplementsInterfaceB
    {
    }
}
