using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Server.WebListener;

namespace HotAddSample
{
    // This sample shows how to dynamically add or remove prefixes for the underlying server.
    // Be careful not to remove the prefix you're currently accessing because the connection
    // will be reset before the end of the request.
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var server = (ServerInformation)app.Server;
            var listener = server.Listener;
            listener.UrlPrefixes.Add("http://localhost:12346/pathBase/");

            app.Use(async (context, next) =>
            {
                // Note: To add any prefix other than localhost you must run this sample as an administrator.
                var toAdd = context.Request.Query["add"];
                if (!string.IsNullOrEmpty(toAdd))
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync("<html><body>");
                    try
                    {
                        listener.UrlPrefixes.Add(toAdd);
                        await context.Response.WriteAsync("Added: " + toAdd);
                    }
                    catch (Exception ex)
                    {
                        await context.Response.WriteAsync("Error adding: " + toAdd + "<br>");
                        await context.Response.WriteAsync(ex.ToString().Replace(Environment.NewLine, "<br>"));
                    }
                    await context.Response.WriteAsync("<br><a href=\"/\">back</a>");
                    await context.Response.WriteAsync("</body></html>");
                    return;
                }
                await next();
            });

            app.Use(async (context, next) =>
            {
                // Be careful not to remove the prefix you're currently accessing because the connection
                // will be reset before the response is sent.
                var toRemove = context.Request.Query["remove"];
                if (!string.IsNullOrEmpty(toRemove))
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync("<html><body>");
                    if (listener.UrlPrefixes.Remove(toRemove))
                    {
                        await context.Response.WriteAsync("Removed: " + toRemove);
                    }
                    else
                    {
                        await context.Response.WriteAsync("Not found: " + toRemove);
                    }
                    await context.Response.WriteAsync("<br><a href=\"/\">back</a>");
                    await context.Response.WriteAsync("</body></html>");
                    return;
                }
                await next();
            });

            app.Run(async context =>
            {
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync("<html><body>");
                await context.Response.WriteAsync("Listening on these prefixes: <br>");
                foreach (var prefix in listener.UrlPrefixes)
                {
                    await context.Response.WriteAsync("<a href=\"" + prefix + "\">" + prefix + "</a> <a href=\"?remove=" + prefix + "\">(remove)</a><br>");
                }

                await context.Response.WriteAsync("<form action=\"/\" method=\"GET\">");
                await context.Response.WriteAsync("<input type=\"text\" name=\"add\" value=\"http://localhost:12348\" >");
                await context.Response.WriteAsync("<input type=\"submit\" value=\"Add\">");
                await context.Response.WriteAsync("</form>");

                await context.Response.WriteAsync("</body></html>");
            });
        }
    }
}
