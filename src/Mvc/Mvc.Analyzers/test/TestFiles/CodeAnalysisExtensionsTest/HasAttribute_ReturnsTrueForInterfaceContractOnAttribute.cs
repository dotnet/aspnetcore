// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public interface IHasAttribute_ReturnsTrueForInterfaceContractOnAttribute { }

    public class HasAttribute_ReturnsTrueForInterfaceContractOnAttribute : Attribute, IHasAttribute_ReturnsTrueForInterfaceContractOnAttribute { }

    [HasAttribute_ReturnsTrueForInterfaceContractOnAttribute]
    public class HasAttribute_ReturnsTrueForInterfaceContractOnAttributeTest
    {
    }

    public class HasAttribute_ReturnsTrueForInterfaceContractOnAttributeDerived : HasAttribute_ReturnsTrueForInterfaceContractOnAttributeTest
    {
    }
}
