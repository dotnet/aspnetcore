using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Tests.Internal
{
    public class MyContainer : IServiceProvider
    {
        private IServiceProvider _inner;
        private IServiceCollection _services;

        public bool FancyMethodCalled { get; private set; }

        public IServiceCollection Services => _services;

        public string Environment { get; set; }

        public object GetService(Type serviceType)
        {
            return _inner.GetService(serviceType);
        }

        public void Populate(IServiceCollection services)
        {
            _services = services;
        }

        public void Build()
        {
            _inner = _services.BuildServiceProvider();
        }

        public void MyFancyContainerMethod()
        {
            FancyMethodCalled = true;
        }
    }
}
