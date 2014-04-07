
using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public interface IViewComponentHelper
    {
        HtmlString Invoke(string name, params object[] args);

        HtmlString Invoke(Type componentType, params object[] args);

        void RenderInvoke(string name, params object[] args);

        void RenderInvoke(Type componentType, params object[] args);

        Task<HtmlString> InvokeAsync(string name, params object[] args);

        Task<HtmlString> InvokeAsync(Type componentType, params object[] args);

        Task RenderInvokeAsync(string name, params object[] args);

        Task RenderInvokeAsync(Type componentType, params object[] args);
    }
}
