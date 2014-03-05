using System;
using System.Collections.Generic;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.NestedProviders;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.Mvc
{
    public class MvcServices
    {
        public static IEnumerable<IServiceDescriptor> GetDefaultServices(IConfiguration configuration,
                                                                         IApplicationEnvironment env)
        {
            yield return DescribeService<IControllerFactory, DefaultControllerFactory>(configuration);
            yield return DescribeService<IControllerDescriptorFactory, DefaultControllerDescriptorFactory>(configuration);
            yield return DescribeService<IActionSelector, DefaultActionSelector>(configuration);
            yield return DescribeService<IActionInvokerFactory, ActionInvokerFactory>(configuration);
            yield return DescribeService<IActionResultHelper, ActionResultHelper>(configuration);
            yield return DescribeService<IActionResultFactory, ActionResultFactory>(configuration);
            yield return DescribeService<IParameterDescriptorFactory, DefaultParameterDescriptorFactory>(configuration);
            yield return DescribeService<IValueProviderFactory, RouteValueValueProviderFactory>(configuration);
            yield return DescribeService<IValueProviderFactory, QueryStringValueProviderFactory>(configuration);
            yield return DescribeService<IControllerAssemblyProvider, AppDomainControllerAssemblyProvider>(configuration);
            yield return DescribeService<IActionDiscoveryConventions, DefaultActionDiscoveryConventions>(configuration);
            yield return DescribeService<IFileSystem>(new PhysicalFileSystem(env.ApplicationBasePath));

            yield return DescribeService<IMvcRazorHost>(new MvcRazorHost(typeof(RazorView).FullName));

#if NET45
            // TODO: Container chaining to flow services from the host to this container

            yield return DescribeService<ICompilationService, CscBasedCompilationService>(configuration);

            // TODO: Make this work like normal when we get container chaining
            // TODO: Update this when we have the new host services
            // AddInstance<ICompilationService>(new RoslynCompilationService(hostServiceProvider));
#endif
            yield return DescribeService<IRazorCompilationService, RazorCompilationService>(configuration);
            yield return DescribeService<IVirtualPathViewFactory, VirtualPathViewFactory>(configuration);
            yield return DescribeService<IViewEngine, RazorViewEngine>(configuration);

            // This is temporary until DI has some magic for it
            yield return DescribeService<INestedProviderManager<ActionDescriptorProviderContext>,
                                         NestedProviderManager<ActionDescriptorProviderContext>>(configuration);
            yield return DescribeService<INestedProviderManager<ActionInvokerProviderContext>,
                                         NestedProviderManager<ActionInvokerProviderContext>>(configuration);
            yield return DescribeService<INestedProvider<ActionDescriptorProviderContext>,
                                         ReflectedActionDescriptorProvider>(configuration);
            yield return DescribeService<INestedProvider<ActionInvokerProviderContext>,
                                         ActionInvokerProvider>(configuration);

            yield return DescribeService<IModelMetadataProvider, DataAnnotationsModelMetadataProvider>(configuration);
            yield return DescribeService<IActionBindingContextProvider, DefaultActionBindingContextProvider>(configuration);

            yield return DescribeService<IValueProviderFactory, RouteValueValueProviderFactory>(configuration);
            yield return DescribeService<IValueProviderFactory, QueryStringValueProviderFactory>(configuration);

            yield return DescribeService<IModelBinder, TypeConverterModelBinder>(configuration);
            yield return DescribeService<IModelBinder, TypeMatchModelBinder>(configuration);
            yield return DescribeService<IModelBinder, GenericModelBinder>(configuration);
            yield return DescribeService<IModelBinder, MutableObjectModelBinder>(configuration);
            yield return DescribeService<IModelBinder, ComplexModelDtoModelBinder>(configuration);

            yield return DescribeService<IInputFormatter, JsonInputFormatter>(configuration);
        }

        public static IServiceDescriptor DescribeService<TService, TImplementation>(
            IConfiguration configuration,
            LifecycleKind lifecycle = LifecycleKind.Transient)
        {
            return DescribeService(typeof(TService), typeof(TImplementation), configuration, lifecycle);
        }

        public static IServiceDescriptor DescribeService<TService>(
            TService implementation,
            LifecycleKind lifecycle = LifecycleKind.Transient)
        {
            return new ServiceTypeDescriptor(typeof(TService), implementation, lifecycle);
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

            public ServiceTypeDescriptor(Type serviceType, object implementation, LifecycleKind lifecycle)
            {
                ServiceType = serviceType;
                ImplementationInstance = implementation;
                Lifecycle = lifecycle;
            }

            public LifecycleKind Lifecycle { get; private set; }
            public Type ServiceType { get; private set; }
            public Type ImplementationType { get; private set; }
            public object ImplementationInstance { get; private set; }
        }
    }
}
