
namespace Microsoft.AspNet.Abstractions.Extensions
{
    /// <summary>
    /// Options for the Map middleware
    /// </summary>
    public class MapOptions
    {
        /// <summary>
        /// The path to match
        /// </summary>
        public PathString PathMatch { get; set; }

        /// <summary>
        /// The branch taken for a positive match
        /// </summary>
        public RequestDelegate Branch { get; set; }
    }
}