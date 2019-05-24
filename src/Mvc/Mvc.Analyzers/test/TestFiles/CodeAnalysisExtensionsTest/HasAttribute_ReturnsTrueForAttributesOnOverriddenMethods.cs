using System;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class HasAttribute_ReturnsTrueForAttributesOnOverriddenMethodsAttribute : Attribute { }

    public class HasAttribute_ReturnsTrueForAttributesOnOverriddenMethodsBase
    {
        [HasAttribute_ReturnsTrueForAttributesOnOverriddenMethodsAttribute]
        public virtual void SomeMethod() { }
    }

    public class HasAttribute_ReturnsTrueForAttributesOnOverriddenMethodsTest : HasAttribute_ReturnsTrueForAttributesOnOverriddenMethodsBase
    {
        public override void SomeMethod() { }
    }
}
