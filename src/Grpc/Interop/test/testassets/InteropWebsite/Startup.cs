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

using System.Reflection;
using Grpc.Testing;
using Microsoft.Net.Http.Headers;

namespace InteropTestsWebsite;

public class Startup
{
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGrpc();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostApplicationLifetime applicationLifetime)
    {
        // Required to notify test infrastructure that it can begin tests
        applicationLifetime.ApplicationStarted.Register(() =>
        {
            Console.WriteLine("Application started.");

            var runtimeVersion = typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
            Console.WriteLine($"NetCoreAppVersion: {runtimeVersion}");
            var aspNetCoreVersion = typeof(HeaderNames).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
            Console.WriteLine($"AspNetCoreAppVersion: {aspNetCoreVersion}");
        });

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<TestServiceImpl>();
        });
    }
}
