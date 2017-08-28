// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting.Fakes
{
    public class FakeServiceCollection : IServiceProvider
    {
        private IServiceProvider _inner;
        private IServiceCollection _services;

        public bool FancyMethodCalled { get; private set; }

        public IServiceCollection Services => _services;

        public string State { get; set; }

        public object GetService(Type serviceType)
        {
            return _inner.GetService(serviceType);
        }

        public void Populate(IServiceCollection services)
        {
            _services = services;
            _services.AddSingleton<FakeServiceCollection>(this);
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
