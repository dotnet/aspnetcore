using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Http
{
    public interface IHttpContextContainer
    {
        DefaultHttpContext HttpContext { get; }
    }
}
