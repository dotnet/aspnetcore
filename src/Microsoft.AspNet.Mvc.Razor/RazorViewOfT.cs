using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Razor
{
    public abstract class RazorView<TModel> : RazorView
    {
        public TModel Model { get; set; }

        public dynamic ViewBag
        {
            get { return ViewData; }
        }

        public ViewData<TModel> ViewData { get; set; }

        public HtmlHelper<TModel> Html { get; set; }

        public override Task RenderAsync(ViewContext context, TextWriter writer)
        {
            var viewData = context.ViewData as ViewData<TModel>;
            ViewData = viewData ?? new ViewData<TModel>(context.ViewData);
            Model = (TModel)ViewData.Model;
            InitHelpers(context);

            return base.RenderAsync(context, writer);
        }

        private void InitHelpers(RequestContext context)
        {
            Html = new HtmlHelper<TModel>(context, ViewData);
        }
    }
}
