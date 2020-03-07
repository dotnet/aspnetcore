using System;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class HasAttribute_ReturnsTrueForAttributesOnPropertiesAttribute : Attribute { }

    public class HasAttribute_ReturnsTrueForAttributesOnProperties
    {
        [HasAttribute_ReturnsTrueForAttributesOnPropertiesAttribute]
        public string SomeProperty { get; set; }
    }
}
