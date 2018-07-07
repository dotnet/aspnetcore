// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class TestServer: IDisposable, IStartup
    {
        private const string InProcessHandlerDll = "aspnetcorev2_inprocess.dll";
        private const string AspNetCoreModuleDll = "aspnetcorev2.dll";
        private const string HWebCoreDll = "hwebcore.dll";

        internal static string HostableWebCoreLocation => Environment.ExpandEnvironmentVariables($@"%windir%\system32\inetsrv\{HWebCoreDll}");

        private static readonly SemaphoreSlim WebCoreLock = new SemaphoreSlim(1, 1);

        // Currently this is hardcoded in HostableWebCore.config
        private static readonly int BasePort = 50691;
        private static readonly Uri BaseUri = new Uri("http://localhost:" + BasePort);

        private readonly TaskCompletionSource<object> _startedTaskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly Action<IApplicationBuilder> _appBuilder;
        private readonly ILoggerFactory _loggerFactory;

        public HttpClient HttpClient { get; }
        public TestConnection CreateConnection() => new TestConnection(BasePort);

        private IWebHost _host;

        private TestServer(Action<IApplicationBuilder> appBuilder, ILoggerFactory loggerFactory)
        {
            _appBuilder = appBuilder;
            _loggerFactory = loggerFactory;

            HttpClient = new HttpClient(new LoggingHandler(new SocketsHttpHandler(), _loggerFactory.CreateLogger<TestServer>()))
            {
                BaseAddress = BaseUri
            };
        }

        public static async Task<TestServer> Create(Action<IApplicationBuilder> appBuilder, ILoggerFactory loggerFactory)
        {
            await WebCoreLock.WaitAsync();
            var server = new TestServer(appBuilder, loggerFactory);
            server.Start();
            await server.HttpClient.GetAsync("/start");
            await server._startedTaskCompletionSource.Task;
            return server;
        }

        public static Task<TestServer> Create(RequestDelegate app, ILoggerFactory loggerFactory)
        {
            return Create(builder => builder.Run(app), loggerFactory);
        }

        private void Start()
        {
            LoadLibrary(HostableWebCoreLocation);
            LoadLibrary(InProcessHandlerDll);
            LoadLibrary(AspNetCoreModuleDll);

            set_main_handler(Main);
            var startResult = WebCoreActivate(Path.GetFullPath("HostableWebCore.config"), null, "Instance");
            if (startResult != 0)
            {
                throw new InvalidOperationException($"Error while running WebCoreActivate: {startResult}");
            }
        }

        private int Main(IntPtr argc, IntPtr argv)
        {
            _host = new WebHostBuilder()
                .UseIIS()
                .ConfigureServices(services => {
                        services.AddSingleton<IStartup>(this);
                        services.AddSingleton<ILoggerFactory>(_loggerFactory);
                    })
                .UseSetting(WebHostDefaults.ApplicationKey, typeof(TestServer).GetTypeInfo().Assembly.FullName)
                .Build();

            var doneEvent = new ManualResetEventSlim();
            var lifetime = _host.Services.GetService<IApplicationLifetime>();

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
            WebCoreShutdown(false);
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
    }
}
