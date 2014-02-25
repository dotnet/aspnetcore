using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    public interface IActionBindingContextProvider
    {
        Task<ActionBindingContext> GetActionBindingContextAsync(ActionContext actionContext);
    }
}
