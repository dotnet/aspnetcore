namespace Microsoft.AspNet.Mvc
{
    public interface IActionConstraint
    {
        bool Accept(RequestContext context);
    }
}
