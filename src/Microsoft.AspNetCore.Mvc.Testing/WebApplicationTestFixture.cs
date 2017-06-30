// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing.Internal;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Mvc.Testing
{
    /// <summary>
    /// Fixture for bootstrapping an application in memory for functional end to end tests.
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
                .UseApplicationAssemblies()
                .UseRequestCulture("en-GB", "en-US")
                .UseStartupCulture("en-GB", "en-US");

            ConfigureApplication(builder);
            _server = builder.Build();

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

        /// <summary>
        /// Gets an instance of the <see cref="HttpClient"/> used to send <see cref="HttpRequestMessage"/> to the server.
        /// </summary>
        public HttpClient Client { get; }

        /// <summary>
        /// Creates a new instance of an <see cref="HttpClient"/> that can be used to
        /// send <see cref="HttpRequestMessage"/> to the server.
        /// </summary>
        /// <returns>The <see cref="HttpClient"/></returns>
        public HttpClient CreateClient()
        {
            var client = _server.CreateClient();
            client.BaseAddress = new Uri("http://localhost");

            return client;
        }

        /// <summary>
        /// Creates a new instance of an <see cref="HttpClient"/> that can be used to
        /// send <see cref="HttpRequestMessage"/> to the server.
        /// </summary>
        /// <param name="baseAddress">The base address of the <see cref="HttpClient"/> instance.</param>
        /// <param name="handlers">A list of <see cref="DelegatingHandler"/> instances to setup on the
        /// <see cref="HttpClient"/>.</param>
        /// <returns>The <see cref="HttpClient"/>.</returns>
        public HttpClient CreateClient(Uri baseAddress, params DelegatingHandler[] handlers)
        {
            if (handlers == null || handlers.Length == 0)
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

        /// <inheritdoc />
        public void Dispose()
        {
            Client.Dispose();
            _server.Dispose();
        }
    }
}
