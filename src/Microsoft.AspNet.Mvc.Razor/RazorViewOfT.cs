using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc.Razor
{
    public abstract class RazorView<TModel> : RazorView
    {
        public TModel Model
        {
            get
            {
                return ViewData == null ? default(TModel) : ViewData.Model;
            }
        }

        public dynamic ViewBag
        {
            get { return ViewData; }
        }

        public ViewData<TModel> ViewData { get; private set; }

        public HtmlHelper<TModel> Html { get; set; }

        public override Task RenderAsync([NotNull] ViewContext context)
        {
            ViewData = context.ViewData as ViewData<TModel>;
            if (ViewData == null)
            {
                if (context.ViewData != null)
                {
                    ViewData = new ViewData<TModel>(context.ViewData);
                }
                else
                {
                    var metadataProvider = context.ServiceProvider.GetService<IModelMetadataProvider>();
                    ViewData = new ViewData<TModel>(metadataProvider);
                }

                // Have new ViewData; make sure it's visible everywhere.
                context.ViewData = ViewData;
            }

            InitHelpers(context);

            return base.RenderAsync(context);
        }

        private void InitHelpers(ViewContext context)
        {
            Html = new HtmlHelper<TModel>(context.HttpContext, ViewData);
        }
    }
}
