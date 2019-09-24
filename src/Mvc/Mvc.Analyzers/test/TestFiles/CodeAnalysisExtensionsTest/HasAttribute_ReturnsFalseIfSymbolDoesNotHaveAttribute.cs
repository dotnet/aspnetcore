using System;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class HasAttribute_ReturnsFalseIfTypeDoesNotHaveAttribute : Attribute { }

    [Controller]
    public class HasAttribute_ReturnsFalseIfTypeDoesNotHaveAttributeTest
    {
        [NonAction]
        public void SomeMethod()
        {

        }

        [BindProperty]
        public string SomeProperty { get; set; }
    }
}
