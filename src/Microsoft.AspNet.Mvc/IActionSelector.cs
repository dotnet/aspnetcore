namespace Microsoft.AspNet.Mvc
{
    public interface IActionSelector
    {
        ActionDescriptor Select(RequestContext context);

        bool Match(ActionDescriptor descriptor, RequestContext context);
    }
}
