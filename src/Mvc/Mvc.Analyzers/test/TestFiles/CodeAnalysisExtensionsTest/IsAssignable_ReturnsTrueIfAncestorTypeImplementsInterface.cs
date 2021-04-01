// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
