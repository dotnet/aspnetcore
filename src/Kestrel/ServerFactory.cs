using Microsoft.AspNet.Hosting.Server;
using System;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.ConfigurationModel;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel;
using System.Collections.Generic;
using Microsoft.Framework.Runtime;

namespace Kestrel
{
    /// <summary>
    /// Summary description for ServerFactory
    /// </summary>
    public class ServerFactory : IServerFactory
    {
        private readonly ILibraryManager _libraryManager;

        public ServerFactory(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        public IServerInformation Initialize(IConfiguration configuration)
        {
            var information = new ServerInformation();
            information.Initialize(configuration);
            return information;
        }

        public IDisposable Start(IServerInformation serverInformation, Func<object, Task> application)
        {
            var disposables = new List<IDisposable>();
            var information = (ServerInformation)serverInformation;
            var engine = new KestrelEngine(_libraryManager);
            engine.Start(1);
            foreach (var address in information.Addresses)
            {
                disposables.Add(engine.CreateServer(
                    address.Scheme,
                    address.Host,
                    address.Port,
                    async frame =>
                    {
                        var request = new ServerRequest(frame);
                        await application.Invoke(request);
                    }));
            }
            disposables.Add(engine);
            return new Disposable(() =>
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            });
        }
    }
}
