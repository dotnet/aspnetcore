// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Http;

namespace StatusCodePagesSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseErrorPage(ErrorPageOptions.ShowAll);
            //app.UseStatusCodePages(); // There is a default response but any of the following can be used to change the behavior.

            // app.WithHandler(context => context.HttpContext.Response.SendAsync("Handler, status code: " + context.HttpContext.Response.StatusCode, "text/plain"));
            // app.WithResponse("text/plain", "Response, status code: {0}");
            // app.WithRedirect("~/errors/{0}"); // PathBase relative
            // app.WithRedirect("/base/errors/{0}"); // Absolute
            // app.WithPipeline(builder => builder.UseWelcomePage());
            // app.WithReExecute("/errors/{0}");

            // "/[?statuscode=400]"
            app.Use((context, next) =>
            {
                if (context.Request.Path.HasValue && !context.Request.Path.Equals(new PathString("/")))
                {
                    return next();
                }

                // Check for ?statuscode=400
                var requestedStatusCode = context.Request.Query["statuscode"];
                if (!string.IsNullOrEmpty(requestedStatusCode))
                {
                    context.Response.StatusCode = int.Parse(requestedStatusCode);
                    return Task.FromResult(0);
                }

                var builder = new StringBuilder();
                builder.AppendLine("<html><body>");
                builder.AppendLine("<a href=\"" +
                    WebUtility.HtmlEncode(context.Request.PathBase.ToString()) + "/missingpage/\">" +
                    WebUtility.HtmlEncode(context.Request.PathBase.ToString()) + "/missingpage/</a><br>");

                for (int statusCode = 400; statusCode < 600; statusCode++)
                {
                    builder.AppendLine("<a href=\"?statuscode=" + statusCode + "\">" + statusCode + "</a><br>");
                }
                builder.AppendLine("</body></html>");
                return context.Response.SendAsync(builder.ToString(), "text/html");
            });

            // "/errors/400"
            app.Use((context, next) =>
            {
                PathString remainder;
                if (context.Request.Path.StartsWithSegments(new PathString("/errors"), out remainder))
                {
                    var builder = new StringBuilder();
                    builder.AppendLine("<html><body>");
                    builder.AppendLine("An error occurred, Status Code: " + WebUtility.HtmlEncode(remainder.ToString().Substring(1)) + "<br>");
                    var referrer = context.Request.Headers["referer"];
                    if (!string.IsNullOrEmpty(referrer))
                    {
                        builder.AppendLine("<a href=\"" + WebUtility.HtmlEncode(referrer) + "\">Retry " + WebUtility.HtmlEncode(referrer) + "</a><br>");
                    }
                    builder.AppendLine("</body></html>");
                    return context.Response.SendAsync(builder.ToString(), "text/html");
                }
                return next();
            });
        }
    }
}
