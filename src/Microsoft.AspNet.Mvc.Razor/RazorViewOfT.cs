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

        public ViewDataDictionary<TModel> ViewData { get; private set; }

        public IHtmlHelper<TModel> Html { get; set; }

        public override Task RenderAsync([NotNull] ViewContext context)
        {
            ViewData = context.ViewData as ViewDataDictionary<TModel>;
            if (ViewData == null)
            {
                if (context.ViewData != null)
                {
                    ViewData = new ViewDataDictionary<TModel>(context.ViewData);
                }
                else
                {
                    var metadataProvider = context.ServiceProvider.GetService<IModelMetadataProvider>();
                    ViewData = new ViewDataDictionary<TModel>(metadataProvider);
                }

                // Have new ViewDataDictionary; make sure it's visible everywhere.
                context.ViewData = ViewData;
            }

            InitHelpers(context);

            return base.RenderAsync(context);
        }

        private void InitHelpers(ViewContext context)
        {
            Html = context.ServiceProvider.GetService<IHtmlHelper<TModel>>();

            var contextable = Html as ICanHasViewContext;
            if (contextable != null)
            {
                contextable.Contextualize(context);
            }
        }
    }
}
