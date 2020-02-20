using Microsoft.AspNetCore.Razor.TagHelpers;

// Used for testing purposes to verify multiple TagHelpers applying to a single element.
namespace SimpleMvc22.TagHelpers
{
    /// <summary>
    /// I made it!
    /// </summary>
    [HtmlTargetElement("environment")]
    public class EnvironmentTagHelper : TagHelper
    {
        /// <summary>
        /// Exclude it!
        /// </summary>
        public string Exclude {get; set;}
    }
}