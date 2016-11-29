using System;

namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    /// <summary>
    /// Describes how to find the JavaScript code that performs prerendering.
    /// </summary>
    public class JavaScriptModuleExport
    {
        /// <summary>
        /// Creates a new instance of <see cref="JavaScriptModuleExport"/>.
        /// </summary>
        /// <param name="moduleName">The path to the JavaScript module containing prerendering code.</param>
        public JavaScriptModuleExport(string moduleName)
        {
            ModuleName = moduleName;
        }

        /// <summary>
        /// Specifies the path to the JavaScript module containing prerendering code.
        /// </summary>
        public string ModuleName { get; private set; }

        /// <summary>
        /// If set, specifies the name of the CommonJS export that is the prerendering function to execute.
        /// If not set, the JavaScript module's default CommonJS export must itself be the prerendering function.
        /// </summary>
        public string ExportName { get; set; }

        /// <summary>
        /// Obsolete. Do not use. Instead, configure Webpack to build a Node.js-compatible bundle and reference that directly.
        /// </summary>
        [Obsolete("Do not use. This feature will be removed. Instead, configure Webpack to build a Node.js-compatible bundle and reference that directly.")]
        public string WebpackConfig { get; set; }
    }
}