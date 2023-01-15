// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Diagnostics;

namespace ExceptionHandlerSample;

public class Startup
{
    public void Configure(IApplicationBuilder app)
    {
        // Configure the error handler to show an error page.
        app.UseExceptionHandler(errorApp =>
        {
            // Normally you'd use MVC or similar to render a nice page.
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync("<html><body>\r\n");
                await context.Response.WriteAsync("We're sorry, we encountered an un-expected issue with your application.<br>\r\n");

                var error = context.Features.Get<IExceptionHandlerFeature>();
                if (error != null)
                {
                    // This error would not normally be exposed to the client
                    await context.Response.WriteAsync("<br>Error: " + HtmlEncoder.Default.Encode(error.Error.Message) + "<br>\r\n");
                }
                await context.Response.WriteAsync("<br><a href=\"/\">Home</a><br>\r\n");
                await context.Response.WriteAsync("</body></html>\r\n");
                await context.Response.WriteAsync(new string(' ', 512)); // Padding for IE
            });
        });

        // We could also configure it to re-execute the request on the normal pipeline with a different path.
        // app.UseExceptionHandler("/error.html");

        // The broken section of our application.
        app.Map("/throw", throwApp =>
        {
            throwApp.Run(context => { throw new Exception("Application Exception"); });
        });

        app.UseStaticFiles();

        // The home page.
        app.Run(async context =>
        {
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync("<html><body>Welcome to the sample<br><br>\r\n");
            await context.Response.WriteAsync("Click here to throw an exception: <a href=\"/throw\">throw</a>\r\n");
            await context.Response.WriteAsync("</body></html>\r\n");
        });
    }
}

