using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleDestination
{
    public class StatusMiddleware
    {
        /// <summary>
        /// Instantiates a new <see cref="StatusMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public StatusMiddleware(RequestDelegate next)
        {
        }

        /// <summary>
        /// Writes the status of the request sent in response. Does not invoke later middleware in the pipeline.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> of the current request.</param>
        /// <returns>A <see cref="Task"/> that completes when writing to the response is done.</returns>
        public Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
                 
            return context.Response.WriteAsync(context.Response.StatusCode.ToString());
        }

    }
}
