// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    public class TestServer: IDisposable, IStartup
    {
        private const string InProcessHandlerDll = "aspnetcorev2_inprocess.dll";
        private const string AspNetCoreModuleDll = "aspnetcorev2.dll";
        private const string HWebCoreDll = "hwebcore.dll";

        internal static string HostableWebCoreLocation => Environment.ExpandEnvironmentVariables($@"%windir%\system32\inetsrv\{HWebCoreDll}");
        internal static string BasePath => Path.Combine(Path.GetDirectoryName(new Uri(typeof(TestServer).Assembly.CodeBase).AbsolutePath),
                                                        "ANCM",
                                                        Environment.Is64BitProcess ? "x64" : "x86");

        internal static string AspNetCoreModuleLocation => Path.Combine(BasePath, AspNetCoreModuleDll);

        private static readonly SemaphoreSlim WebCoreLock = new SemaphoreSlim(1, 1);

        private static readonly int PortRetryCount = 10;

        private readonly TaskCompletionSource<object> _startedTaskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly Action<IApplicationBuilder> _appBuilder;
        private readonly ILoggerFactory _loggerFactory;
        private readonly hostfxr_main_fn _hostfxrMainFn;
        
        private Uri BaseUri => new Uri("http://localhost:" + _currentPort);
        public HttpClient HttpClient { get; private set; }
        public TestConnection CreateConnection() => new TestConnection(_currentPort);

        private static IISServerOptions _options;
        private IWebHost _host;

        private string _appHostConfigPath;
        private int _currentPort;

        private TestServer(Action<IApplicationBuilder> appBuilder, ILoggerFactory loggerFactory)
        {
            _hostfxrMainFn = Main;
            _appBuilder = appBuilder;
            _loggerFactory = loggerFactory;
        }

        public static async Task<TestServer> Create(Action<IApplicationBuilder> appBuilder, ILoggerFactory loggerFactory, IISServerOptions options)
        {
            await WebCoreLock.WaitAsync();
            _options = options;
            var server = new TestServer(appBuilder, loggerFactory);
            server.Start();
            (await server.HttpClient.GetAsync("/start")).EnsureSuccessStatusCode();
            await server._startedTaskCompletionSource.Task;
            return server;
        }

        public static Task<TestServer> Create(RequestDelegate app, ILoggerFactory loggerFactory)
        {
            return Create(builder => builder.Run(app), loggerFactory, new IISServerOptions());
        }

        public static Task<TestServer> Create(RequestDelegate app, ILoggerFactory loggerFactory, IISServerOptions options)
        {
            return Create(builder => builder.Run(app), loggerFactory, options);
        }

        private void Start()
        {
            LoadLibrary(HostableWebCoreLocation);
            _appHostConfigPath = Path.GetTempFileName();
            
            set_main_handler(_hostfxrMainFn);

            Retry(() =>
            {
                _currentPort = TestPortHelper.GetNextPort();

                InitializeConfig(_currentPort);

                var startResult = WebCoreActivate(_appHostConfigPath, null, "Instance");
                if (startResult != 0)
                {
                    throw new InvalidOperationException($"Error while running WebCoreActivate: {startResult} on port {_currentPort}");
                }
            }, PortRetryCount);

            HttpClient = new HttpClient(new LoggingHandler(new SocketsHttpHandler(), _loggerFactory.CreateLogger<TestServer>()))
            {
                BaseAddress = BaseUri
            };
        }

        private void InitializeConfig(int port)
        {
            var webHostConfig = XDocument.Load(Path.GetFullPath("HostableWebCore.config"));
            webHostConfig.XPathSelectElement("/configuration/system.webServer/globalModules/add[@name='AspNetCoreModuleV2']")
                .SetAttributeValue("image", AspNetCoreModuleLocation);

            var siteElement = webHostConfig.Root
                .RequiredElement("system.applicationHost")
                .RequiredElement("sites")
                .RequiredElement("site");

            siteElement
                .RequiredElement("bindings")
                .RequiredElement("binding")
                .SetAttributeValue("bindingInformation", $":{port}:localhost");

            webHostConfig.Save(_appHostConfigPath);
        }

        private int Main(IntPtr argc, IntPtr argv)
        {
            var builder = new WebHostBuilder()
                .UseIIS()
                .ConfigureServices(services =>
                {
                    services.Configure<IISServerOptions>(options => options.MaxRequestBodySize = _options.MaxRequestBodySize);
                    services.AddSingleton<IStartup>(this);
                    services.AddSingleton(_loggerFactory);
                })
                .UseSetting(WebHostDefaults.ApplicationKey, typeof(TestServer).GetTypeInfo().Assembly.FullName);
            _host = builder.Build();

            var doneEvent = new ManualResetEventSlim();
            var lifetime = _host.Services.GetService<IHostApplicationLifetime>();

            lifetime.ApplicationStopping.Register(() => doneEvent.Set());
            _host.Start();
            _startedTaskCompletionSource.SetResult(null);
            doneEvent.Wait();
            _host.Dispose();
            return 0;
        }

        public void Dispose()
        {
            HttpClient.Dispose();

            // WebCoreShutdown occasionally AVs
            // This causes the dotnet test process to crash
            // To avoid this, we have to wait to shutdown 
            // and pass in true to immediately shutdown the hostable web core
            // Both of these seem to be required.
            Thread.Sleep(100);
            WebCoreShutdown(immediate: true);
            WebCoreLock.Release();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return services.BuildServiceProvider();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Map("/start", builder => builder.Run(context => context.Response.WriteAsync("Done")));
            _appBuilder(app);
        }

        private delegate int hostfxr_main_fn(IntPtr argc, IntPtr argv);

        [DllImport(HWebCoreDll)]
        private static extern int WebCoreActivate(
            [In, MarshalAs(UnmanagedType.LPWStr)]
            string appHostConfigPath,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            string rootWebConfigPath,
            [In, MarshalAs(UnmanagedType.LPWStr)]
            string instanceName);

        [DllImport(HWebCoreDll)]
        private static extern int WebCoreShutdown(bool immediate);

        [DllImport(InProcessHandlerDll)]
        private static extern int set_main_handler(hostfxr_main_fn main);

        [DllImport("kernel32", SetLastError=true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

        private void Retry(Action func, int attempts)
        {
            var exceptions = new List<Exception>();

            for (var attempt = 0; attempt < attempts; attempt++)
            {
                try
                {
                    func();
                    return;
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            throw new AggregateException(exceptions);
        }
    }
}
