// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace TestServer;

public class ExternalNavigationStartup
{
    public void Configure(IApplicationBuilder app)
    {
        app.Run(async context =>
        {
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(
                """
                <!DOCTYPE html>
                <html>
                <body>
                    <h1 id="external-navigation-target">External navigation target</h1>
                </body>
                </html>
                """);
        });
    }
}
