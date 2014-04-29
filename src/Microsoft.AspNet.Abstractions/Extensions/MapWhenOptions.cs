using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Abstractions.Extensions
{
    /// <summary>
    /// Options for the MapWhen middleware
    /// </summary>
    public class MapWhenOptions
    {
        /// <summary>
        /// The user callback that determines if the branch should be taken
        /// </summary>
        public Func<HttpContext, bool> Predicate { get; set; }

        /// <summary>
        /// The async user callback that determines if the branch should be taken
        /// </summary>
        public Func<HttpContext, Task<bool>> PredicateAsync { get; set; }

        /// <summary>
        /// The branch taken for a positive match
        /// </summary>
        public RequestDelegate Branch { get; set; }
    }
}