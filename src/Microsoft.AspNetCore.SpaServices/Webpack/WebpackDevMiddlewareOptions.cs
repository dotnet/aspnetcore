using System.Collections.Generic;

namespace Microsoft.AspNetCore.SpaServices.Webpack
{
    public class WebpackDevMiddlewareOptions
    {
        public bool HotModuleReplacement { get; set; }
        public int HotModuleReplacementServerPort { get; set; }
        public bool ReactHotModuleReplacement { get; set; }
        public string ConfigFile { get; set; }
        public string ProjectPath { get; set; }
        public IDictionary<string, string> EnvironmentVariables { get; set; }
    }
}