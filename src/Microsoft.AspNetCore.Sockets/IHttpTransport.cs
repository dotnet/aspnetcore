using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Sockets
{
    public interface IHttpTransport
    {
        /// <summary>
        /// Executes the transport
        /// </summary>
        /// <param name="context"></param>
        /// <returns>A <see cref="Task"/> that completes when the transport has finished processing</returns>
        Task ProcessRequest(HttpContext context);

        /// <summary>
        /// Completes the Task returned from ProcessRequest if not already complete
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();
    }
}
