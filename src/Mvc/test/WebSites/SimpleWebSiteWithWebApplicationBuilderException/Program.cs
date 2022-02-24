// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var app = WebApplication.Create(args);

app.MapGet("/", (Func<string>)(() => "Hello World"));

throw new InvalidOperationException("This application failed to start");

namespace SimpleWebSiteWithWebApplicationBuilderException
{
    public partial class Program
    {
    }
}
