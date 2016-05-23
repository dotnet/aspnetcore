namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    public class JavaScriptModuleExport
    {
        public JavaScriptModuleExport(string moduleName)
        {
            this.ModuleName = moduleName;
        }

        public string ModuleName { get; private set; }
        public string ExportName { get; set; }
        public string WebpackConfig { get; set; }
    }
}