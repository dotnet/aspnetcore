using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.HttpLogging
{
    /// <summary>
    /// 
    /// </summary>
    public class HttpResponseLoggingContext
    {
        internal HttpResponseLoggingContext(HttpContext context, HttpLoggingOptions options, IHeaderDictionary headers)
        {
            HttpContext = context;
            Options = options;
            Headers = headers;
        }

        /// <summary>
        /// 
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <summary>
        /// 
        /// </summary>
        public HttpLoggingOptions Options { get; }

        /// <summary>
        /// 
        /// </summary>
        public string? StatusCode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IHeaderDictionary Headers { get; }

        private List<(string, string)>? _extra;

        /// <summary>
        /// 
        /// </summary>
        public List<(string, string)> Extra
        {
            get
            {
                _extra ??= new List<(string, string)>();
                return _extra;
            }
        }
    }
}
