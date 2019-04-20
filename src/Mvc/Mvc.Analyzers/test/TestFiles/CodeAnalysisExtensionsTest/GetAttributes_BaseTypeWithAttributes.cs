namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [ApiConventionType(typeof(object))]
    [ApiController]
    [ApiConventionType(typeof(string))]
    public class GetAttributes_BaseTypeWithAttributesBase
    {
    }

    [ApiConventionType(typeof(int))]
    public class GetAttributes_BaseTypeWithAttributesDerived : GetAttributes_BaseTypeWithAttributesBase
    {
    }
}
