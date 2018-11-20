using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace SampleStartups
{
    // We can't reference real servers in this sample without creating a circular repo dependency.
    // This fake server lets us at least run the code.
    public class FakeServer : IServer
    {
        public IFeatureCollection Features => new FeatureCollection();

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void Dispose()
        {
        }
    }

    public static class FakeServerWebHostBuilderExtensions
    {
        public static IWebHostBuilder UseFakeServer(this IWebHostBuilder builder)
        {
            return builder.ConfigureServices(services => services.AddSingleton<IServer, FakeServer>());
        }
    }
}
