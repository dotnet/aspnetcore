using System;
using System.Collections.Generic;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Security.DataProtection;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingServices
    {
        public static IEnumerable<IServiceDescriptor> GetDefaultServices()
        {
            return GetDefaultServices(new Configuration());
        }

        public static IEnumerable<IServiceDescriptor> GetDefaultServices(IConfiguration configuration)
        {
            yield return DescribeService<IHostingEngine, HostingEngine>(configuration);
            yield return DescribeService<IServerManager, ServerManager>(configuration);

            yield return DescribeService<IStartupManager, StartupManager>(configuration);
            yield return DescribeService<IStartupLoaderProvider, StartupLoaderProvider>(configuration);

            yield return DescribeService<IBuilderFactory, BuilderFactory>(configuration);
            yield return DescribeService<IHttpContextFactory, HttpContextFactory>(configuration);

            yield return DescribeService<ITypeActivator, TypeActivator>(configuration);

            // The default IDataProtectionProvider is a singleton.
            // Note: DPAPI isn't usable in IIS where the user profile hasn't been loaded, but loading DPAPI
            // is deferred until the first call to Protect / Unprotect. It's up to an IIS-based host to
            // replace this service as part of application initialization.
            yield return new ServiceDescriptor {
              ServiceType = typeof(IDataProtectionProvider),
              Lifecycle = LifecycleKind.Singleton,
              ImplementationInstance = DataProtectionProvider.CreateFromDpapi()
            };
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