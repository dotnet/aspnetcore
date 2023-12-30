// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

public partial class TestServer : IDisposable
{
    private const string InProcessHandlerDll = "aspnetcorev2_inprocess.dll";
    private const string AspNetCoreModuleDll = "aspnetcorev2.dll";
    private const string HWebCoreDll = "hwebcore.dll";

    internal static string HostableWebCoreLocation => Environment.ExpandEnvironmentVariables($@"%windir%\system32\inetsrv\{HWebCoreDll}");
    internal static string BasePath => Path.Combine(Path.GetDirectoryName(typeof(TestServer).Assembly.Location),
                                                    "ANCM",
                                                    Environment.Is64BitProcess ? "x64" : "x86");

    internal static string AspNetCoreModuleLocation => Path.Combine(BasePath, AspNetCoreModuleDll);

    private static readonly SemaphoreSlim WebCoreLock = new SemaphoreSlim(1, 1);

    private static readonly int PortRetryCount = 10;

    private readonly TaskCompletionSource _startedTaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly Action<IApplicationBuilder> _appBuilder;
    private readonly ILoggerFactory _loggerFactory;
    private readonly hostfxr_main_fn _hostfxrMainFn;
    private readonly bool _isHttps;
    private readonly string _protocol;

    private Uri BaseUri => new Uri(_protocol + "://localhost:" + _currentPort);
    public HttpClient HttpClient { get; private set; }
    public TestConnection CreateConnection() => new TestConnection(_currentPort);

    private static IISServerOptions _options;
    private IHost _host;

    private string _appHostConfigPath;
    private int _currentPort;

    private TestServer(Action<IApplicationBuilder> appBuilder, ILoggerFactory loggerFactory, bool isHttps)
    {
        _hostfxrMainFn = Main;
        _appBuilder = appBuilder;
        _loggerFactory = loggerFactory;
        _isHttps = isHttps;
        _protocol = isHttps ? "https" : "http";
    }

    public static async Task<TestServer> Create(Action<IApplicationBuilder> appBuilder, ILoggerFactory loggerFactory, IISServerOptions options, bool isHttps = false)
    {
        await WebCoreLock.WaitAsync();
        _options = options;
        var server = new TestServer(appBuilder, loggerFactory, isHttps);
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

    public static Task<TestServer> CreateHttps(RequestDelegate app, ILoggerFactory loggerFactory)
    {
        return Create(builder => builder.Run(app), loggerFactory, new IISServerOptions(), isHttps: true);
    }

    private void Start()
    {
        LoadLibrary(HostableWebCoreLocation);
        _appHostConfigPath = Path.GetTempFileName();

        set_main_handler(_hostfxrMainFn);

        Retry(() =>
        {
            _currentPort = _isHttps ? TestPortHelper.GetNextSSLPort() : TestPortHelper.GetNextPort();

            InitializeConfig(_currentPort);

            var startResult = WebCoreActivate(_appHostConfigPath, null, "Instance");
            if (startResult != 0)
            {
                throw new InvalidOperationException($"Error while running WebCoreActivate: {startResult} on port {_currentPort}");
            }
        }, PortRetryCount);

        HttpClient = new HttpClient(new LoggingHandler(new SocketsHttpHandler(), _loggerFactory.CreateLogger<TestServer>()))
        {
            BaseAddress = BaseUri,
            Timeout = TimeSpan.FromSeconds(200),
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

        var binding = siteElement
            .RequiredElement("bindings")
            .RequiredElement("binding");

        binding.SetAttributeValue("protocol", _protocol);
        binding.SetAttributeValue("bindingInformation", $":{port}:localhost");

        webHostConfig.Save(_appHostConfigPath);
    }

    private int Main(IntPtr argc, IntPtr argv)
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseIIS()
                    .UseSetting(WebHostDefaults.ApplicationKey, typeof(TestServer).GetTypeInfo().Assembly.FullName)
                    .Configure(app =>
                    {
                        app.Map("/start", builder => builder.Run(context => context.Response.WriteAsync("Done")));
                        _appBuilder(app);
                    })
                    .ConfigureServices(services =>
                    {
                        services.Configure<IISServerOptions>(options => options.MaxRequestBodySize = _options.MaxRequestBodySize);
                        services.AddSingleton(_loggerFactory);
                    });
            })
            .Build();

        var doneEvent = new ManualResetEventSlim();
        var lifetime = _host.Services.GetService<IHostApplicationLifetime>();

        lifetime.ApplicationStopping.Register(() => doneEvent.Set());
        _host.Start();
        _startedTaskCompletionSource.SetResult();
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

    private delegate int hostfxr_main_fn(IntPtr argc, IntPtr argv);

    [LibraryImport(HWebCoreDll)]
    private static partial int WebCoreActivate(
        [MarshalAs(UnmanagedType.LPWStr)]
            string appHostConfigPath,
        [MarshalAs(UnmanagedType.LPWStr)]
            string rootWebConfigPath,
        [MarshalAs(UnmanagedType.LPWStr)]
            string instanceName);

    [LibraryImport(HWebCoreDll)]
    private static partial int WebCoreShutdown([MarshalAs(UnmanagedType.Bool)] bool immediate);

    [LibraryImport(InProcessHandlerDll)]
    private static partial int set_main_handler(hostfxr_main_fn main);

    [LibraryImport("kernel32", EntryPoint = "LoadLibraryW", SetLastError = true)]
    private static partial IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

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
