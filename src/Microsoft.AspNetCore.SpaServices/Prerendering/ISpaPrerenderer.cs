using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    /// <summary>
    /// Represents a service that can perform server-side prerendering for
    /// JavaScript-based Single Page Applications. This is an alternative
    /// to using the 'asp-prerender-module' tag helper.
    /// </summary>
    public interface ISpaPrerenderer
    {
        /// <summary>
        /// Invokes JavaScript code to perform server-side prerendering for a
        /// Single-Page Application. This is an alternative to using the
        /// 'asp-prerender-module' tag helper.
        /// </summary>
        /// <param name="moduleName">The JavaScript module that exports a prerendering function.</param>
        /// <param name="exportName">The name of the export from the JavaScript module, if it is not the default export.</param>
        /// <param name="customDataParameter">An optional JSON-serializable object to pass to the JavaScript prerendering function.</param>
        /// <param name="timeoutMilliseconds">If specified, the prerendering task will time out after this duration if not already completed.</param>
        /// <returns></returns>
        Task<RenderToStringResult> RenderToString(
            string moduleName,
            string exportName = null,
            object customDataParameter = null,
            int timeoutMilliseconds = default(int));
    }
}
