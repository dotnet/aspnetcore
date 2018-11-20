using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericWebHost
{
    internal class WebHostService : IHostedService
    {
        public WebHostService(IOptions<WebHostServiceOptions> options, IServiceProvider services, HostBuilderContext hostBuilderContext, IServer server,
            ILogger<WebHostService> logger, DiagnosticListener diagnosticListener, IHttpContextFactory httpContextFactory)
        {
            Options = options?.Value ?? throw new System.ArgumentNullException(nameof(options));

            if (Options.ConfigureApp == null)
            {
                throw new ArgumentException(nameof(Options.ConfigureApp));
            }

            Services = services ?? throw new ArgumentNullException(nameof(services));
            HostBuilderContext = hostBuilderContext ?? throw new ArgumentNullException(nameof(hostBuilderContext));
            Server = server ?? throw new ArgumentNullException(nameof(server));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            DiagnosticListener = diagnosticListener ?? throw new ArgumentNullException(nameof(diagnosticListener));
            HttpContextFactory = httpContextFactory ?? throw new ArgumentNullException(nameof(httpContextFactory));
        }

        public WebHostServiceOptions Options { get; }
        public IServiceProvider Services { get; }
        public HostBuilderContext HostBuilderContext { get; }
        public IServer Server { get; }
        public ILogger<WebHostService> Logger { get; }
        public DiagnosticListener DiagnosticListener { get; }
        public IHttpContextFactory HttpContextFactory { get; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Server.Features.Get<IServerAddressesFeature>()?.Addresses.Add("http://localhost:5000");

            var builder = new ApplicationBuilder(Services, Server.Features);
            Options.ConfigureApp(HostBuilderContext, builder);
            var app = builder.Build();

            var httpApp = new HostingApplication(app, Logger, DiagnosticListener, HttpContextFactory);
            return Server.StartAsync(httpApp, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Server.StopAsync(cancellationToken);
        }
    }
}