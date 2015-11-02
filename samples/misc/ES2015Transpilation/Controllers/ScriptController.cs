using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.NodeServices;

namespace ES2015Example.Controllers
{
    public class ScriptController : Controller
    {
        private INodeServices nodeServices;
        
        public ScriptController(INodeServices nodeServices) {
            this.nodeServices = nodeServices;
        }
        
        public async Task<ContentResult> Transpile(string filename)
        {
            // TODO: Don't hard-code wwwroot; use proper path conversions
            var fileContents = System.IO.File.ReadAllText("wwwroot/" + filename);
            var transpiledResult = await this.nodeServices.Invoke("transpilation.js", fileContents, Request.Path.Value);
            return Content(transpiledResult, "application/javascript");
        }
    }
}
