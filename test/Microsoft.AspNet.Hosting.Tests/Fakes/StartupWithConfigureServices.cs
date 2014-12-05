using System;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting.Fakes
{
    public class StartupWithConfigureServices
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IFoo, Foo>();
        }

        public void Configure(IApplicationBuilder app, IFoo foo)
        {
            foo.Bar();
        }

        public interface IFoo
        {
            bool Invoked { get; }
            void Bar();
        }

        public class Foo : IFoo
        {
            public bool Invoked { get; private set; }

            public void Bar()
            {
                Invoked = true;
            }
        }
    }
}