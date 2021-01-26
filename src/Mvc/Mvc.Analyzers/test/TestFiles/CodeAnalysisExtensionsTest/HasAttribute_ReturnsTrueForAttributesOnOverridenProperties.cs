using System;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class HasAttribute_ReturnsTrueForAttributesOnOverriddenPropertiesAttribute : Attribute { }

    public class HasAttribute_ReturnsTrueForAttributesOnOverriddenPropertiesBase
    {
        [HasAttribute_ReturnsTrueForAttributesOnOverriddenPropertiesAttribute]
        public virtual string SomeProperty { get; set; }
    }

    public class HasAttribute_ReturnsTrueForAttributesOnOverriddenProperties : HasAttribute_ReturnsTrueForAttributesOnOverriddenPropertiesBase
    {
        public override string SomeProperty { get; set; }
    }
}
