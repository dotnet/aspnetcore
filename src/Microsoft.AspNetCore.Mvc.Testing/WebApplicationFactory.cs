// Copyright (c) .NET  Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.AspNetCore.Mvc.Testing
{
    /// <summary>
    /// Factory for bootstrapping an application in memory for functional end to end tests.
    /// </summary>
    /// <typeparam name="TEntryPoint">A type in the entry point assembly of the application.
    /// Typically the Startup or Program classes can be used.</typeparam>
    public class WebApplicationFactory<TEntryPoint> : IDisposable where TEntryPoint : class
    {
        private TestServer _server;
        private Action<IWebHostBuilder> _configuration;
        private IList<HttpClient> _clients = new List<HttpClient>();
        private List<WebApplicationFactory<TEntryPoint>> _derivedFactories =
            new List<WebApplicationFactory<TEntryPoint>>();

        /// <summary>
        /// <para>
        /// Creates an instance of <see cref="WebApplicationFactory{TEntryPoint}"/>. This factory can be used to
        /// create a <see cref="TestServer"/> instance using the MVC application defined by <typeparamref name="TEntryPoint"/>
        /// and one or more <see cref="HttpClient"/> instances used to send <see cref="HttpRequestMessage"/> to the <see cref="TestServer"/>.
        /// The <see cref="WebApplicationFactory{TEntryPoint}"/> will find the entry point class of <typeparamref name="TEntryPoint"/>
        /// assembly and initialize the application by calling <c>IWebHostBuilder CreateWebHostBuilder(string [] args)</c>
        /// on <typeparamref name="TEntryPoint"/>.
        /// </para>
        /// <para>
        /// This constructor will infer the application content root path by searching for a
        /// <see cref="WebApplicationFactoryContentRootAttribute"/> on the assembly containing the functional tests with
        /// a key equal to the <typeparamref name="TEntryPoint"/> assembly <see cref="Assembly.FullName"/>.
        /// In case an attribute with the right key can't be found, <see cref="WebApplicationFactory{TEntryPoint}"/>
        /// will fall back to searching for a solution file (*.sln) and then appending <typeparamref name="TEntryPoint"/> asembly name
        /// to the solution directory. The application root directory will be used to discover views and content files.
        /// </para>
        /// <para>
        /// The application assemblies will be loaded from the dependency context of the assembly containing
        /// <typeparamref name="TEntryPoint" />. This means that project dependencies of the assembly containing
        /// <typeparamref name="TEntryPoint" /> will be loaded as application assemblies.
        /// </para>
        /// </summary>
        public WebApplicationFactory()
        {
            _configuration = ConfigureWebHost;
        }

        /// <summary>
        /// Gets the <see cref="TestServer"/> created by this <see cref="WebApplicationFactory{TEntryPoint}"/>.
        /// </summary>
        public TestServer Server => _server;

        /// <summary>
        /// Gets the <see cref="IReadOnlyList{WebApplicationFactory}"/> of factories created from this factory
        /// by further customizing the <see cref="IWebHostBuilder"/> when calling 
        /// <see cref="WebApplicationFactory{TEntryPoint}.WithWebHostBuilder(Action{IWebHostBuilder})"/>.
        /// </summary>
        public IReadOnlyList<WebApplicationFactory<TEntryPoint>> Factories => _derivedFactories.AsReadOnly();

        /// <summary>
        /// Gets the <see cref="WebApplicationFactoryClientOptions"/> used by <see cref="CreateClient()"/>.
        /// </summary>
        public WebApplicationFactoryClientOptions ClientOptions { get; private set; } = new WebApplicationFactoryClientOptions();

        /// <summary>
        /// Creates a new <see cref="WebApplicationFactory{TEntryPoint}"/> with a <see cref="IWebHostBuilder"/>
        /// that is further customized by <paramref name="configuration"/>.
        /// </summary>
        /// <param name="configuration">
        /// An <see cref="Action{IWebHostBuilder}"/> to configure the <see cref="IWebHostBuilder"/>.
        /// </param>
        /// <returns>A new <see cref="WebApplicationFactory{TEntryPoint}"/>.</returns>
        public WebApplicationFactory<TEntryPoint> WithWebHostBuilder(Action<IWebHostBuilder> configuration)
        {
            var factory = new DelegatedWebApplicationFactory(
                ClientOptions,
                CreateServer,
                CreateWebHostBuilder,
                GetTestAssemblies,
                builder =>
                {
                    _configuration(builder);
                    configuration(builder);
                });

            _derivedFactories.Add(factory);

            return factory;
        }

        private void EnsureServer()
        {
            if (_server != null)
            {
                return;
            }

            EnsureDepsFile();


            var builder = CreateWebHostBuilder();
            SetContentRoot(builder);
            _configuration(builder);
            _server = CreateServer(builder);
        }

        private void SetContentRoot(IWebHostBuilder builder)
        {
            var metadataAttributes = GetContentRootMetadataAttributes(
                typeof(TEntryPoint).Assembly.FullName,
                typeof(TEntryPoint).Assembly.GetName().Name);

            string contentRoot = null;
            for (var i = 0; i < metadataAttributes.Length; i++)
            {
                var contentRootAttribute = metadataAttributes[i];
                var contentRootCandidate = Path.Combine(
                    AppContext.BaseDirectory,
                    contentRootAttribute.ContentRootPath);

                var contentRootMarker = Path.Combine(
                    contentRootCandidate,
                    Path.GetFileName(contentRootAttribute.ContentRootTest));

                if (File.Exists(contentRootMarker))
                {
                    contentRoot = contentRootCandidate;
                    break;
                }
            }

            if (contentRoot != null)
            {
                builder.UseContentRoot(contentRoot);
            }
            else
            {
                builder.UseSolutionRelativeContentRoot(typeof(TEntryPoint).Assembly.GetName().Name);
            }
        }

        private WebApplicationFactoryContentRootAttribute[] GetContentRootMetadataAttributes(
            string tEntryPointAssemblyFullName,
            string tEntryPointAssemblyName)
        {
            var testAssembly = GetTestAssemblies();
            var metadataAttributes = testAssembly
                .SelectMany(a => a.GetCustomAttributes<WebApplicationFactoryContentRootAttribute>())
                .Where(a => string.Equals(a.Key, tEntryPointAssemblyFullName, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(a.Key, tEntryPointAssemblyName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.Priority)
                .ToArray();

            return metadataAttributes;
        }

        /// <summary>
        /// Gets the assemblies containing the functional tests. The
        /// <see cref="WebApplicationFactoryContentRootAttribute"/> applied to these
        /// assemblies defines the content root to use for the given
        /// <typeparamref name="TEntryPoint"/>.
        /// </summary>
        /// <returns>The list of <see cref="Assembly"/> containing tests.</returns>
        protected virtual IEnumerable<Assembly> GetTestAssemblies()
        {
            try
            {
                // The default dependency context will be populated in .net core applications.
                var context = DependencyContext.Default;
                if (context != null)
                {
                    // Find the list of projects
                    var projects = context.CompileLibraries.Where(l => l.Type == "project");

                    // Find the list of projects runtime information and their assembly names.
                    var runtimeProjectLibraries = context.RuntimeLibraries
                        .Where(r => projects.Any(p => p.Name == r.Name))
                        .ToDictionary(r => r, r => r.GetDefaultAssemblyNames(context).ToArray());

                    var entryPointAssemblyName = typeof(TEntryPoint).Assembly.GetName().Name;

                    // Find the project containing TEntryPoint
                    var entryPointRuntimeLibrary = runtimeProjectLibraries
                        .Single(rpl => rpl.Value.Any(a => string.Equals(a.Name, entryPointAssemblyName, StringComparison.Ordinal)));

                    // Find the list of projects referencing TEntryPoint.
                    var candidates = runtimeProjectLibraries
                        .Where(rpl => rpl.Key.Dependencies
                            .Any(d => string.Equals(d.Name, entryPointRuntimeLibrary.Key.Name, StringComparison.Ordinal)));

                    return candidates.SelectMany(rl => rl.Value).Select(Assembly.Load);
                }
                else
                {
                    // The app domain friendly name will be populated in full framework.
                    return new[] { Assembly.Load(AppDomain.CurrentDomain.FriendlyName) };
                }
            }
            catch (Exception)
            {
            }

            return Array.Empty<Assembly>();
        }

        private void EnsureDepsFile()
        {
            var depsFileName = $"{typeof(TEntryPoint).Assembly.GetName().Name}.deps.json";
            var depsFile = new FileInfo(Path.Combine(AppContext.BaseDirectory, depsFileName));
            if (!depsFile.Exists)
            {
                throw new InvalidOperationException(Resources.FormatMissingDepsFile(
                    depsFile.FullName,
                    Path.GetFileName(depsFile.FullName)));
            }
        }

        /// <summary>
        /// Creates a <see cref="IWebHostBuilder"/> used to set up <see cref="TestServer"/>.
        /// </summary>
        /// <remarks>
        /// The default implementation of this method looks for a <c>public static IWebHostBuilder CreateDefaultBuilder(string[] args)</c>
        /// method defined on the entry point of the assembly of <typeparamref name="TEntryPoint" /> and invokes it passing an empty string
        /// array as arguments.
        /// </remarks>
        /// <returns>A <see cref="IWebHostBuilder"/> instance.</returns>
        protected virtual IWebHostBuilder CreateWebHostBuilder() =>
            WebHostBuilderFactory.CreateFromTypesAssemblyEntryPoint<TEntryPoint>(Array.Empty<string>()) ??
            throw new InvalidOperationException(Resources.FormatMissingCreateWebHostBuilderMethod(
                nameof(IWebHostBuilder),
                typeof(TEntryPoint).Assembly.EntryPoint.DeclaringType.FullName,
                typeof(WebApplicationFactory<TEntryPoint>).Name,
                nameof(CreateWebHostBuilder)));

        /// <summary>
        /// Creates the <see cref="TestServer"/> with the bootstrapped application in <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IWebHostBuilder"/> used to
        /// create the server.</param>
        /// <returns>The <see cref="TestServer"/> with the bootstrapped application.</returns>
        protected virtual TestServer CreateServer(IWebHostBuilder builder) => new TestServer(builder);

        /// <summary>
        /// Gives a fixture an opportunity to configure the application before it gets built.
        /// </summary>
        /// <param name="builder">The <see cref="IWebHostBuilder"/> for the application.</param>
        protected virtual void ConfigureWebHost(IWebHostBuilder builder)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="HttpClient"/> that automatically follows
        /// redirects and handles cookies.
        /// </summary>
        /// <returns>The <see cref="HttpClient"/>.</returns>
        public HttpClient CreateClient() =>
            CreateClient(ClientOptions);

        /// <summary>
        /// Creates an instance of <see cref="HttpClient"/> that automatically follows
        /// redirects and handles cookies.
        /// </summary>
        /// <returns>The <see cref="HttpClient"/>.</returns>
        public HttpClient CreateClient(WebApplicationFactoryClientOptions options) =>
            CreateDefaultClient(options.BaseAddress, options.CreateHandlers());

        /// <summary>
        /// Creates a new instance of an <see cref="HttpClient"/> that can be used to
        /// send <see cref="HttpRequestMessage"/> to the server. The base address of the <see cref="HttpClient"/>
        /// instance will be set to <c>http://localhost</c>.
        /// </summary>
        /// <param name="handlers">A list of <see cref="DelegatingHandler"/> instances to set up on the
        /// <see cref="HttpClient"/>.</param>
        /// <returns>The <see cref="HttpClient"/>.</returns>
        public HttpClient CreateDefaultClient(params DelegatingHandler[] handlers) =>
            CreateDefaultClient(new Uri("http://localhost"), handlers);

        /// <summary>
        /// Creates a new instance of an <see cref="HttpClient"/> that can be used to
        /// send <see cref="HttpRequestMessage"/> to the server.
        /// </summary>
        /// <param name="baseAddress">The base address of the <see cref="HttpClient"/> instance.</param>
        /// <param name="handlers">A list of <see cref="DelegatingHandler"/> instances to set up on the
        /// <see cref="HttpClient"/>.</param>
        /// <returns>The <see cref="HttpClient"/>.</returns>
        public HttpClient CreateDefaultClient(Uri baseAddress, params DelegatingHandler[] handlers)
        {
            EnsureServer();
            if (handlers == null || handlers.Length == 0)
            {
                var client = _server.CreateClient();
                client.BaseAddress = baseAddress;

                return client;
            }
            else
            {
                for (var i = handlers.Length - 1; i > 0; i--)
                {
                    handlers[i - 1].InnerHandler = handlers[i];
                }

                var serverHandler = _server.CreateHandler();
                handlers[handlers.Length - 1].InnerHandler = serverHandler;

                var client = new HttpClient(handlers[0])
                {
                    BaseAddress = baseAddress
                };

                _clients.Add(client);

                return client;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var client in _clients)
            {
                client.Dispose();
            }

            foreach (var factory in _derivedFactories)
            {
                factory.Dispose();
            }

            _server?.Dispose();
        }

        private class DelegatedWebApplicationFactory : WebApplicationFactory<TEntryPoint>
        {
            private readonly Func<IWebHostBuilder, TestServer> _createServer;
            private readonly Func<IWebHostBuilder> _createWebHostBuilder;
            private readonly Func<IEnumerable<Assembly>> _getTestAssemblies;

            public DelegatedWebApplicationFactory(
                WebApplicationFactoryClientOptions options,
                Func<IWebHostBuilder, TestServer> createServer,
                Func<IWebHostBuilder> createWebHostBuilder,
                Func<IEnumerable<Assembly>> getTestAssemblies,
                Action<IWebHostBuilder> configureWebHost)
            {
                ClientOptions = new WebApplicationFactoryClientOptions(options);
                _createServer = createServer;
                _createWebHostBuilder = createWebHostBuilder;
                _getTestAssemblies = getTestAssemblies;
                _configuration = configureWebHost;
            }

            protected override TestServer CreateServer(IWebHostBuilder builder) => _createServer(builder);

            protected override IWebHostBuilder CreateWebHostBuilder() => _createWebHostBuilder();

            protected override IEnumerable<Assembly> GetTestAssemblies() => _getTestAssemblies();

            protected override void ConfigureWebHost(IWebHostBuilder builder) => _configuration(builder);
        }
    }
}
