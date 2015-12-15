// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Antiforgery;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.OptionsModel;

namespace AntiforgerySample
{
    public class FormPostSampleMiddleware
    {
        private readonly IAntiforgery _antiforgery;
        private readonly AntiforgeryOptions _options;
        private readonly RequestDelegate _next;

        public FormPostSampleMiddleware(
            RequestDelegate next,
            IAntiforgery antiforgery,
            IOptions<AntiforgeryOptions> options)
        {
            _next = next;
            _antiforgery = antiforgery;
            _options = options.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Method == "GET")
            {
                var page =
@"<html>
<body>
<form action=""/"" method=""post"">
<input type=""text"" name=""{0}"" value=""{1}""/>
<input type=""submit"" />
</form>
</body>
</html>";

                var tokenSet = _antiforgery.GetAndStoreTokens(context);
                await context.Response.WriteAsync(string.Format(page, _options.FormFieldName, tokenSet.RequestToken));
            }
            else if (context.Request.Method == "POST")
            {
                // This will throw if invalid.
                await _antiforgery.ValidateRequestAsync(context);

                var page =
@"<html>
<body>
<h1>Everything is fine</h1>
<h2><a href=""/"">Try Again</a></h2>
</form>
</body>
</html>";
                await context.Response.WriteAsync(page);
            }
            else
            {
                await _next(context);
            }
        }
    }
}
