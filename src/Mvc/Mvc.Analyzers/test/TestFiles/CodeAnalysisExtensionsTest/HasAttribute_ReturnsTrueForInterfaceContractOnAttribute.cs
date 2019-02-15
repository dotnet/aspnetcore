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
