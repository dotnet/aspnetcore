using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Razor
{
    public abstract class RazorView<TModel> : RazorView
    {
        public TModel Model { get; set; }

        public dynamic ViewData { get; set; }

        public override Task RenderAsync(ViewContext context, TextWriter writer)
        {
            ViewData = context.ViewData;
            Model = (TModel)ViewData.Model;
            return base.RenderAsync(context, writer);
        }
    }
}
