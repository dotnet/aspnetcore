// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Diagnostics;

namespace StatusCodePagesSample;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddProblemDetails();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();
        app.UseStatusCodePages(); // There is a default response but any of the following can be used to change the behavior.

        // app.UseStatusCodePages(context => context.HttpContext.Response.SendAsync("Handler, status code: " + context.HttpContext.Response.StatusCode, "text/plain"));
        // app.UseStatusCodePages("text/plain", "Response, status code: {0}");
        // app.UseStatusCodePagesWithRedirects("~/errors/{0}"); // PathBase relative
        // app.UseStatusCodePagesWithRedirects("/base/errors/{0}"); // Absolute
        // app.UseStatusCodePages(builder => builder.UseWelcomePage());
        // app.UseStatusCodePagesWithReExecute("/errors/{0}");

        // "/[?statuscode=400]"
        app.Use(async (context, next) =>
        {
            // Check for ?statuscode=400
            var requestedStatusCode = context.Request.Query["statuscode"];
            if (!string.IsNullOrEmpty(requestedStatusCode))
            {
                context.Response.StatusCode = int.Parse(requestedStatusCode, CultureInfo.InvariantCulture);

                // To turn off the StatusCode feature - For example the below code turns off the StatusCode middleware
                // if the query contains a disableStatusCodePages=true parameter.
                var disableStatusCodePages = context.Request.Query["disableStatusCodePages"];
                if (disableStatusCodePages == "true")
                {
                    var statusCodePagesFeature = context.Features.Get<IStatusCodePagesFeature>();
                    if (statusCodePagesFeature != null)
                    {
                        statusCodePagesFeature.Enabled = false;
                    }
                }

                await Task.FromResult(0);
            }
            else
            {
                await next(context);
            }
        });

        // "/errors/400"
        app.Map("/errors", error =>
        {
            error.Run(async context =>
            {
                var builder = new StringBuilder();
                builder.AppendLine("<html><body>");
                builder.AppendLine("An error occurred, Status Code: " + HtmlEncoder.Default.Encode(context.Request.Path.ToString().Substring(1)) + "<br>");
                var referrer = context.Request.Headers["referer"];
                if (!string.IsNullOrEmpty(referrer))
                {
                    builder.AppendLine("<a href=\"" + HtmlEncoder.Default.Encode(referrer) + "\">Retry " + WebUtility.HtmlEncode(referrer) + "</a><br>");
                }
                builder.AppendLine("</body></html>");
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(builder.ToString());
            });
        });

        app.Run(async context =>
        {
            // Generates the HTML with all status codes.
            var builder = new StringBuilder();
            builder.AppendLine("<html><body>");
            builder.AppendLine("<a href=\"" +
                HtmlEncoder.Default.Encode(context.Request.PathBase.ToString()) + "/missingpage/\">" +
                HtmlEncoder.Default.Encode(context.Request.PathBase.ToString()) + "/missingpage/</a><br>");

            var space = string.Concat(Enumerable.Repeat("&nbsp;", 12));
            builder.AppendFormat(CultureInfo.InvariantCulture, "<br><b>{0}{1}{2}</b><br>", "Status Code", space, "Status Code Pages");
            for (int statusCode = 400; statusCode < 600; statusCode++)
            {
                builder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "{0}{1}{2}{3}<br>",
                    statusCode,
                    space + space,
                    string.Format(CultureInfo.InvariantCulture, "<a href=\"?statuscode={0}\">[Enabled]</a>{1}", statusCode, space),
                    string.Format(CultureInfo.InvariantCulture, "<a href=\"?statuscode={0}&disableStatusCodePages=true\">[Disabled]</a>{1}", statusCode, space));
            }

            builder.AppendLine("</body></html>");
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(builder.ToString());
        });
    }

    public static Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>();
            }).Build();

        return host.RunAsync();
    }
}
