
namespace Microsoft.AspNet.Mvc
{
    public interface IUrlHelper
    {
        string Action(string action, string controller, object values);

        string Route(object values);
    }
}
