using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [DefaultStatusCode(StatusCodes.Status412PreconditionFailed)]
    public class TestActionResultUsingStatusCodesConstants { }

    [DefaultStatusCode((int)HttpStatusCode.Redirect)]
    public class TestActionResultUsingHttpStatusCodeCast { }
}
