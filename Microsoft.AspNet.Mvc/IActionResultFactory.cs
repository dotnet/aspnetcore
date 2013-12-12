
namespace Microsoft.AspNet.Mvc
{
    public interface IActionResultFactory
    {
        IActionResult CreateActionResult(object actionReturnValue);
    }
}
