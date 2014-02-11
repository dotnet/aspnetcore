using System;
using System.Collections.Generic;
using Microsoft.AspNet.Configuration;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Hosting.Server;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingServices
    {
        public static IEnumerable<IServiceDescriptor> GetDefaultServices()
        {
            return GetDefaultServices(new EmptyConfiguration());
        }

        public static IEnumerable<IServiceDescriptor> GetDefaultServices(IConfiguration configuration)
        {
            yield return DescribeService<IHostingEngine, HostingEngine>(configuration);
            yield return DescribeService<IServerFactoryProvider, ServerFactoryProvider>(configuration);

            yield return DescribeService<IStartupManager, StartupManager>(configuration);
            yield return DescribeService<IStartupLoaderProvider, StartupLoaderProvider>(configuration);

            yield return DescribeService<IBuilderFactory, BuilderFactory>(configuration);
            yield return DescribeService<IHttpContextFactory, HttpContextFactory>(configuration);
        }

        public static IServiceDescriptor DescribeService<TService, TImplementation>(IConfiguration configuration,
            LifecycleKind lifecycle = LifecycleKind.Transient)
        {
            return DescribeService(typeof(TService), typeof(TImplementation), configuration, lifecycle);
        }

        public static IServiceDescriptor DescribeService(
            Type serviceType,
            Type implementationType,
            IConfiguration configuration,
            LifecycleKind lifecycle)
        {
            var serviceTypeName = serviceType.FullName;
            var implementationTypeName = configuration.Get(serviceTypeName);
            if (!String.IsNullOrEmpty(implementationTypeName))
            {
                try
                {
                    implementationType = Type.GetType(implementationTypeName);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("TODO: unable to locate implementation {0} for service {1}", implementationTypeName, serviceTypeName), ex);
                }
            }
            return new ServiceTypeDescriptor(serviceType, implementationType, lifecycle);
        }

        public class EmptyConfiguration : IConfiguration
        {
            public string Get(string key)
            {
                return null;
            }
        }

        public class ServiceTypeDescriptor : IServiceDescriptor
        {
            public ServiceTypeDescriptor(Type serviceType, Type implementationType, LifecycleKind lifecycle)
            {
                ServiceType = serviceType;
                ImplementationType = implementationType;
                Lifecycle = lifecycle;
            }

            public LifecycleKind Lifecycle { get; private set; }
            public Type ServiceType { get; private set; }
            public Type ImplementationType { get; private set; }
            public object ImplementationInstance { get; private set; }
        }
    }
}