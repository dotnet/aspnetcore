using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Specifies what HTTP methods an action supports.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class AcceptVerbsAttribute : Attribute, IActionHttpMethodProvider
    {
        private readonly IEnumerable<string> _httpMethods;

        /// <summary>
        /// Initializes a new instance of the <see cref="AcceptVerbsAttribute" /> class.
        /// </summary>
        /// <param name="method">The HTTP method the action supports.</param>
        public AcceptVerbsAttribute([NotNull] string method)
            : this(new string[] { method })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcceptVerbsAttribute" /> class.
        /// </summary>
        /// <param name="methods">The HTTP methods the action supports.</param>
        public AcceptVerbsAttribute(params string[] methods)
        {
            // TODO: This assumes that the methods are exactly same as standard Http Methods.
            // The Http Abstractions should take care of these.
            _httpMethods = methods.Select(method => method.ToUpperInvariant());
        }
      
        /// <summary>
        /// Gets the HTTP methods the action supports.
        /// </summary>
        public IEnumerable<string> HttpMethods
        {
            get
            {
                return _httpMethods;
            }
        }
    }
}