#region Copyright notice and license

// Copyright 2019 The gRPC Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace InteropTestsWebsite;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(builder =>
            {
                builder.AddConsole(loggerOptions =>
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    loggerOptions.DisableColors = true;
#pragma warning restore CS0618 // Type or member is obsolete
                });
                builder.SetMinimumLevel(LogLevel.Trace);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel((context, options) =>
                {
                    // Support --port and --use_tls cmdline arguments normally supported
                    // by gRPC interop servers.
                    var useTls = context.Configuration.GetValue("use_tls", false);

                    options.Limits.MinRequestBodyDataRate = null;
                    options.ListenAnyIP(0, listenOptions =>
                    {
                        Console.WriteLine($"Enabling connection encryption: {useTls}");

                        if (useTls)
                        {
                            var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
                            var certPath = Path.Combine(basePath!, "Certs", "server1.pfx");

                            listenOptions.UseHttps(certPath, "1111");
                        }
                        listenOptions.Protocols = HttpProtocols.Http2;
                    });
                });
                webBuilder.UseStartup<Startup>();
            });
}
