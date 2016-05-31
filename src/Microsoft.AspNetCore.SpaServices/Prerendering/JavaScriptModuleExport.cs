namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    public class JavaScriptModuleExport
    {
        public JavaScriptModuleExport(string moduleName)
        {
            ModuleName = moduleName;
        }

        public string ModuleName { get; private set; }
        public string ExportName { get; set; }
        public string WebpackConfig { get; set; }
    }
}