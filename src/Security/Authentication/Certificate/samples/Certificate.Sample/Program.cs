// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace Certificate.Sample;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>()
                    .ConfigureKestrel(options =>
                     {
                         options.ConfigureHttpsDefaults(opt =>
                         {
                             opt.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                         });
                     });
            });
}
