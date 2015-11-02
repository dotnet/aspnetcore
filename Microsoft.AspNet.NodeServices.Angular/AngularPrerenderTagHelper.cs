using System.Threading.Tasks;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.NodeServices;
using Microsoft.AspNet.Http.Extensions;

namespace Microsoft.AspNet.NodeServices.Angular
{
    [HtmlTargetElement(Attributes = PrerenderModuleAttributeName)]
    public class AngularRunAtServerTagHelper : TagHelper
    {
        static StringAsTempFile nodeScript;
        
        static AngularRunAtServerTagHelper() {
            // Consider populating this lazily
            var script = EmbeddedResourceReader.Read(typeof (AngularRunAtServerTagHelper), "/Content/Node/angular-rendering.js");
            nodeScript = new StringAsTempFile(script); // Will be cleaned up on process exit
        }
        
        const string PrerenderModuleAttributeName = "aspnet-ng2-prerender-module";
        const string PrerenderExportAttributeName = "aspnet-ng2-prerender-export";
        
        [HtmlAttributeName(PrerenderModuleAttributeName)]
        public string ModuleName { get; set; }
        
        [HtmlAttributeName(PrerenderExportAttributeName)]
        public string ExportName { get; set; }

        private IHttpContextAccessor contextAccessor;
        private INodeServices nodeServices;

        public AngularRunAtServerTagHelper(INodeServices nodeServices, IHttpContextAccessor contextAccessor)
        {
            this.contextAccessor = contextAccessor;
            this.nodeServices = nodeServices;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var result = await this.nodeServices.InvokeExport(nodeScript.FileName, "renderComponent", new {
                componentModule = this.ModuleName,
                componentExport = this.ExportName,
                tagName = output.TagName,
                baseUrl = UriHelper.GetEncodedUrl(this.contextAccessor.HttpContext.Request)
            });
            output.SuppressOutput();
            output.PostElement.AppendEncoded(result);
        }
    }
}
