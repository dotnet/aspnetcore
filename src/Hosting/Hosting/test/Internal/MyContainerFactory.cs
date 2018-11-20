using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Tests.Internal
{
    public class MyContainerFactory : IServiceProviderFactory<MyContainer>
    {
        public MyContainer CreateBuilder(IServiceCollection services)
        {
            var container = new MyContainer();
            container.Populate(services);
            return container;
        }

        public IServiceProvider CreateServiceProvider(MyContainer containerBuilder)
        {
            containerBuilder.Build();
            return containerBuilder;
        }
    }
}
