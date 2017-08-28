using System;
using Microsoft.Extensions.DependencyInjection;

namespace GenericHostSample
{
    internal class MyContainerFactory : IServiceProviderFactory<MyContainer>
    {
        public MyContainer CreateBuilder(IServiceCollection services)
        {
            return new MyContainer();
        }

        public IServiceProvider CreateServiceProvider(MyContainer containerBuilder)
        {
            throw new NotImplementedException();
        }
    }
}