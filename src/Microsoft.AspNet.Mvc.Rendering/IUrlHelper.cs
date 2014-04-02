namespace Microsoft.AspNet.Mvc.Rendering
{
    public interface IUrlHelper
    {
        string Action(string action, string controller, object values);

        string Route(object values);

        string Content(string contentPath);
    }
}
