
namespace Microsoft.AspNet.Mvc
{
    public interface IRenderUrl
    {
        string Action(string action, string controller, object values);

        string Route(object values);
    }
}
