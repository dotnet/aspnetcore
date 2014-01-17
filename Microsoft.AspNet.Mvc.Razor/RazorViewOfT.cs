using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Razor
{
    public abstract class RazorView<TModel> : RazorView
    {
        public TModel Model { get; set; }

        public override Task RenderAsync(ViewContext context, TextWriter writer)
        {
            Model = (TModel)context.Model;
            return base.RenderAsync(context, writer);
        }
    }
}
