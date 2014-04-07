namespace Microsoft.AspNet.Mvc
{
    public interface IUrlHelper
    {
        string Action(string action, string controller, object values, string protocol, string host, string fragment);

        string Content(string contentPath);
        
        string RouteUrl(object values, string protocol, string host, string fragment);
    }
}
