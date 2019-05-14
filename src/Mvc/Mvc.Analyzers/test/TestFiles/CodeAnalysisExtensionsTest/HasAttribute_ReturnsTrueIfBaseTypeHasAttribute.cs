namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [Controller]
    public class HasAttribute_ReturnsTrueIfBaseTypeHasAttributeBase { }

    public class HasAttribute_ReturnsTrueIfBaseTypeHasAttribute : HasAttribute_ReturnsTrueIfBaseTypeHasAttributeBase { }
}
