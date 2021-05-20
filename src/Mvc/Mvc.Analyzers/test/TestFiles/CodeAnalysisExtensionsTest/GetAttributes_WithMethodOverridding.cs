namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class GetAttributes_WithInheritFalse_ReturnsAllAttributesOnCurrentActionBase
    {
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public virtual void Method() { }
    }

    public class GetAttributes_WithInheritFalse_ReturnsAllAttributesOnCurrentActionClass : GetAttributes_WithInheritFalse_ReturnsAllAttributesOnCurrentActionBase
    {
        [ProducesResponseType(400)]
        public override void Method() { }
    }
}
