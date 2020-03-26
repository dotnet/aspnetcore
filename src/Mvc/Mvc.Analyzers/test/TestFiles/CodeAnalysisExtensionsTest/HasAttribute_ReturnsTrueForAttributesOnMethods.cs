using System;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class HasAttribute_ReturnsTrueForAttributesOnMethodsAttribute : Attribute { }

    public class HasAttribute_ReturnsTrueForAttributesOnMethodsTest
    {
        [HasAttribute_ReturnsTrueForAttributesOnMethodsAttribute]
        public void SomeMethod() { }
    }
}
