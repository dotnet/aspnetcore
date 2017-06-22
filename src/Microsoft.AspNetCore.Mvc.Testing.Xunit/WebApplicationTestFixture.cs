// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Xunit.Internal;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    /// <summary>
    /// XUnit fixture for bootstrapping an application in memory for functional end to end tests.
    /// </summary>
    /// <typeparam name="TStartup">The applications startup class.</typeparam>
    public class WebApplicationTestFixture<TStartup> : IDisposable where TStartup : class
    {
        private readonly TestServer _server;

        public WebApplicationTestFixture()
            : this("src")
        {
        }

        protected WebApplicationTestFixture(string solutionRelativePath)
            : this("*.sln", solutionRelativePath)
        {
        }

        protected WebApplicationTestFixture(string solutionSearchPattern, string solutionRelativePath)
        {
            var startupAssembly = typeof(TStartup).GetTypeInfo().Assembly;

            // This step assumes project name = assembly name.
            var projectName = startupAssembly.GetName().Name;
            var projectPath = Path.Combine(solutionRelativePath, projectName);
            var builder = new MvcWebApplicationBuilder<TStartup>()
                .UseSolutionRelativeContentRoot(projectPath)
                .UseApplicationAssemblies();

            ConfigureApplication(builder);

            var xunitRunnerJson = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "xunit.runner.json"));
            if (!xunitRunnerJson.Exists)
            {
                Console.WriteLine("Can't find xunit.runner.json. " +
                    "Functional tests require '\"shadowCopy\": false' to work properly. " +
                    "Make sure your XUnit configuration has that setup.");
            }

            var content = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(xunitRunnerJson.FullName));
            if (!content.TryGetValue("shadowCopy", out var token) || !(bool)token)
            {
                Console.WriteLine("'shadowCopy' is not set to true on xunit.runner.json. " +
                    "Functional tests require '\"shadowCopy\": false' to work properly. " +
                    "Make sure your XUnit configuration has that setup.");
            }

            using (new CultureReplacer())
            {
                _server = builder.Build();
            }

            Client = _server.CreateClient();
            Client.BaseAddress = new Uri("http://localhost");
        }

        /// <summary>
        /// Gives a fixture an opportunity to configure the application before it gets built.
        /// </summary>
        /// <param name="builder">The <see cref="MvcWebApplicationBuilder{TStartup}"/> for the application.</param>
        protected virtual void ConfigureApplication(MvcWebApplicationBuilder<TStartup> builder)
        {
            builder.ConfigureAfterStartup(s => s.TryAddEnumerable(ServiceDescriptor.Transient<IStartupFilter, CultureReplacerStartupFilter>()));
        }

        public HttpClient Client { get; }

        public HttpClient CreateClient()
        {
            var client = _server.CreateClient();
            client.BaseAddress = new Uri("http://localhost");

            return client;
        }

        public HttpClient CreateClient(Uri baseAddress, params DelegatingHandler[] handlers)
        {
            if (handlers.Length == 0)
            {
                var client = _server.CreateClient();
                client.BaseAddress = baseAddress;

                return client;
            }
            else
            {

                for (var i = handlers.Length - 1; i > 1; i++)
                {
                    handlers[i - 1].InnerHandler = handlers[i];
                }

                var serverHandler = _server.CreateHandler();
                handlers[handlers.Length - 1].InnerHandler = serverHandler;
                var client = new HttpClient(handlers[0]);
                client.BaseAddress = baseAddress;

                return client;
            }
        }

        public void Dispose()
        {
            Client.Dispose();
            _server.Dispose();
        }
    }
}
