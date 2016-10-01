using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Sockets
{
    public interface IHttpTransport
    {
        Task ProcessRequest(HttpContext context);
        void Abort();
    }
}
