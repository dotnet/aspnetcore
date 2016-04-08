using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    [HtmlTargetElement(Attributes = PrerenderModuleAttributeName)]
    public class PrerenderTagHelper : TagHelper
    {
        static INodeServices fallbackNodeServices; // Used only if no INodeServices was registered with DI

        const string PrerenderModuleAttributeName = "asp-prerender-module";
        const string PrerenderExportAttributeName = "asp-prerender-export";
        const string PrerenderWebpackConfigAttributeName = "asp-prerender-webpack-config";

        [HtmlAttributeName(PrerenderModuleAttributeName)]
        public string ModuleName { get; set; }

        [HtmlAttributeName(PrerenderExportAttributeName)]
        public string ExportName { get; set; }

        [HtmlAttributeName(PrerenderWebpackConfigAttributeName)]
        public string WebpackConfigPath { get; set; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        private string applicationBasePath;
        private INodeServices nodeServices;

        public PrerenderTagHelper(IServiceProvider serviceProvider)
        {
            var hostEnv = (IHostingEnvironment)serviceProvider.GetService(typeof (IHostingEnvironment));
            this.nodeServices = (INodeServices)serviceProvider.GetService(typeof (INodeServices)) ?? fallbackNodeServices;
            this.applicationBasePath = hostEnv.ContentRootPath;

            // Consider removing the following. Having it means you can get away with not putting app.AddNodeServices()
            // in your startup file, but then again it might be confusing that you don't need to.
            if (this.nodeServices == null) {
                this.nodeServices = fallbackNodeServices = Configuration.CreateNodeServices(new NodeServicesOptions {
                    HostingModel = NodeHostingModel.Http,
                    ProjectPath = this.applicationBasePath
                });
            }
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var request = this.ViewContext.HttpContext.Request;
            var result = await Prerenderer.RenderToString(
                applicationBasePath: this.applicationBasePath,
                nodeServices: this.nodeServices,
                bootModule: new JavaScriptModuleExport(this.ModuleName) {
                    exportName = this.ExportName,
                    webpackConfig = this.WebpackConfigPath
                },
                requestAbsoluteUrl: UriHelper.GetEncodedUrl(request),
                requestPathAndQuery: request.Path + request.QueryString.Value);
            output.Content.SetHtmlContent(result.Html);

            // Also attach any specified globals to the 'window' object. This is useful for transferring
            // general state between server and client.
            if (result.Globals != null) {
                var stringBuilder = new StringBuilder();
                foreach (var property in result.Globals.Properties()) {
                    stringBuilder.AppendFormat("window.{0} = {1};",
                        property.Name,
                        property.Value.ToString(Formatting.None));
                }
                if (stringBuilder.Length > 0) {
                    output.PostElement.SetHtmlContent($"<script>{ stringBuilder.ToString() }</script>");
                }
            }
        }
    }
}
