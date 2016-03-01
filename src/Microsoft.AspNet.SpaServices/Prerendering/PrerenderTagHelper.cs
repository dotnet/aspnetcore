using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Extensions;
using Microsoft.AspNet.NodeServices;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SpaServices.Prerendering
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

        private IHttpContextAccessor contextAccessor;
        private INodeServices nodeServices;

        public PrerenderTagHelper(IServiceProvider serviceProvider, IHttpContextAccessor contextAccessor)
        {
            this.contextAccessor = contextAccessor;
            this.nodeServices = (INodeServices)serviceProvider.GetService(typeof (INodeServices)) ?? fallbackNodeServices;
            
            // Consider removing the following. Having it means you can get away with not putting app.AddNodeServices()
            // in your startup file, but then again it might be confusing that you don't need to.
            if (this.nodeServices == null) {
                var appEnv = (IApplicationEnvironment)serviceProvider.GetService(typeof(IApplicationEnvironment));
                this.nodeServices = fallbackNodeServices = Configuration.CreateNodeServices(new NodeServicesOptions {
                    HostingModel = NodeHostingModel.Http,
                    ProjectPath = appEnv.ApplicationBasePath
                });
            }
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var request = this.contextAccessor.HttpContext.Request;
            var result = await Prerenderer.RenderToString(
                nodeServices: this.nodeServices,
                bootModule: new JavaScriptModuleExport(this.ModuleName) {
                    exportName = this.ExportName,
                    webpackConfig = this.WebpackConfigPath
                },
                requestAbsoluteUrl: UriHelper.GetEncodedUrl(this.contextAccessor.HttpContext.Request),
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
