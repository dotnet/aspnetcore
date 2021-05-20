// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Cors;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class MvcServiceCollectionExtensionsTest
    {
        // Some MVC services can be registered multiple times, for example, 'IConfigureOptions<MvcOptions>' can
        // be registered by calling 'ConfigureMvc(...)' before the call to 'AddMvc()' in which case the options
        // configuration is run in the order they were registered.
        //
        // For these kind of multi registration service types, we want to make sure that MVC will still add its
        // services if the implementation type is different.
        [Fact]
        public void AddMvc_MultiRegistrationServiceTypes_AreRegistered_MultipleTimes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IWebHostEnvironment>(GetHostingEnvironment());
            RegisterMockMultiRegistrationServices(services);

            // Act
            services.AddMvc();

            // Assert
            VerifyMultiRegistrationServices(services);
        }

        private void RegisterMockMultiRegistrationServices(IServiceCollection services)
        {
            // Register a mock implementation of each service, AddMvcServices should add another implementation.
            foreach (var serviceType in MultiRegistrationServiceTypes)
            {
                var mockType = typeof(Mock<>).MakeGenericType(serviceType.Key);
                services.Add(ServiceDescriptor.Transient(serviceType.Key, mockType));
            }
        }

        private void VerifyMultiRegistrationServices(IServiceCollection services)
        {
            foreach (var serviceType in MultiRegistrationServiceTypes)
            {
                AssertServiceCountEquals(services, serviceType.Key, serviceType.Value.Length + 1);

                foreach (var implementationType in serviceType.Value)
                {
                    AssertContainsSingle(services, serviceType.Key, implementationType);
                }
            }
        }

        [Fact]
        public void AddMvc_SingleRegistrationServiceTypes_AreNotRegistered_MultipleTimes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IWebHostEnvironment>(GetHostingEnvironment());
            RegisterMockSingleRegistrationServices(services);

            // Act
            services.AddMvc();

            // Assert
            VerifySingleRegistrationServices(services);
        }

        [Fact]
        public void AddControllers_AddRazorPages_SingleRegistrationServiceTypes_AreNotRegistered_MultipleTimes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IWebHostEnvironment>(GetHostingEnvironment());
            RegisterMockSingleRegistrationServices(services);

            // Act
            services.AddControllers();
            services.AddRazorPages();

            // Assert
            VerifySingleRegistrationServices(services);
        }

        [Fact]
        public void AddControllersWithViews_SingleRegistrationServiceTypes_AreNotRegistered_MultipleTimes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IWebHostEnvironment>(GetHostingEnvironment());
            RegisterMockSingleRegistrationServices(services);

            // Act
            services.AddControllers();

            // Assert
            VerifySingleRegistrationServices(services);
        }

        [Fact]
        public void AddRazorPages_SingleRegistrationServiceTypes_AreNotRegistered_MultipleTimes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IWebHostEnvironment>(GetHostingEnvironment());
            RegisterMockSingleRegistrationServices(services);

            // Act
            services.AddRazorPages();

            // Assert
            VerifySingleRegistrationServices(services);
        }

        private void RegisterMockSingleRegistrationServices(IServiceCollection services)
        {
            // Register a mock implementation of each service, AddMvcServices should not replace it.
            foreach (var serviceType in SingleRegistrationServiceTypes)
            {
                var mockType = typeof(Mock<>).MakeGenericType(serviceType);
                services.Add(ServiceDescriptor.Transient(serviceType, mockType));
            }
        }

        private void VerifySingleRegistrationServices(IServiceCollection services)
        {
            foreach (var singleRegistrationType in SingleRegistrationServiceTypes)
            {
                AssertServiceCountEquals(services, singleRegistrationType, 1);
            }
        }

        [Fact]
        public void AddMvc_Twice_DoesNotAddDuplicates()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IWebHostEnvironment>(GetHostingEnvironment());

            // Act
            services.AddMvc();
            services.AddMvc();

            // Assert
            VerifyAllServices(services);
        }

        [Fact]
        public void AddControllersAddRazorPages_Twice_DoesNotAddDuplicates()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IWebHostEnvironment>(GetHostingEnvironment());

            // Act
            services.AddControllers();
            services.AddRazorPages();
            services.AddControllers();
            services.AddRazorPages();

            // Assert
            VerifyAllServices(services);
        }

        [Fact]
        public void AddControllersWithViews_Twice_DoesNotAddDuplicates()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IWebHostEnvironment>(GetHostingEnvironment());

            // Act
            services.AddControllersWithViews();
            services.AddControllersWithViews();

            // Assert
            VerifyAllServices(services);
        }

        [Fact]
        public void AddRazorPages_Twice_DoesNotAddDuplicates()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IWebHostEnvironment>(GetHostingEnvironment());

            // Act
            services.AddRazorPages();
            services.AddRazorPages();

            // Assert
            VerifyAllServices(services);
        }

        [Fact]
        public void AddControllersWithViews_AddsDocumentedServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddControllersWithViews();

            // Assert
            // Adds controllers
            Assert.Contains(services, s => s.ServiceType == typeof(IActionInvokerProvider) && s.ImplementationType == typeof(ControllerActionInvokerProvider));
            // Adds ApiExplorer
            Assert.Contains(services, s => s.ServiceType == typeof(IApiDescriptionGroupCollectionProvider));
            // Adds CORS
            Assert.Contains(services, s => s.ServiceType == typeof(CorsAuthorizationFilter));
            // Adds DataAnnotations
            Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<MvcOptions>) && s.ImplementationType == typeof(MvcDataAnnotationsMvcOptionsSetup));
            // Adds FormatterMappings
            Assert.Contains(services, s => s.ServiceType == typeof(FormatFilter));
            // Adds Views
            Assert.Contains(services, s => s.ServiceType == typeof(IHtmlHelper));
            // Adds Razor
            Assert.Contains(services, s => s.ServiceType == typeof(IRazorViewEngine));
            // Adds CacheTagHelper
            Assert.Contains(services, s => s.ServiceType == typeof(CacheTagHelperMemoryCacheFactory));

            // No Razor Pages
            Assert.Empty(services.Where(s => s.ServiceType == typeof(IActionInvokerProvider) && s.ImplementationType == typeof(PageActionInvokerProvider)));
        }

        private void VerifyAllServices(IServiceCollection services)
        {
            var singleRegistrationServiceTypes = SingleRegistrationServiceTypes;
            foreach (var service in services)
            {
                if (singleRegistrationServiceTypes.Contains(service.ServiceType))
                {
                    // 'single-registration' services should only have one implementation registered.
                    AssertServiceCountEquals(services, service.ServiceType, 1);
                }
                else if (service.ImplementationType != null && !service.ImplementationType.Assembly.FullName.Contains("Mvc"))
                {
                    // Ignore types that don't come from MVC
                }
                else
                {
                    // 'multi-registration' services should only have one *instance* of each implementation registered.
                    AssertContainsSingle(services, service.ServiceType, service.ImplementationType);
                }
            }
        }

        [Fact]
        public void AddMvc_AddsAssemblyPartsForFrameworkTagHelpers()
        {
            // Arrange
            var mvcRazorAssembly = typeof(UrlResolutionTagHelper).Assembly;
            var mvcTagHelpersAssembly = typeof(InputTagHelper).Assembly;
            var services = new ServiceCollection();
            var providers = new IApplicationFeatureProvider[]
            {
                new ControllerFeatureProvider(),
                new ViewComponentFeatureProvider()
            };

            // Act
            services.AddMvc();

            // Assert
            var descriptor = Assert.Single(services, d => d.ServiceType == typeof(ApplicationPartManager));
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
            Assert.NotNull(descriptor.ImplementationInstance);
            var manager = Assert.IsType<ApplicationPartManager>(descriptor.ImplementationInstance);

            Assert.Equal(2, manager.ApplicationParts.Count);
            Assert.Single(manager.ApplicationParts.OfType<AssemblyPart>(), p => p.Assembly == mvcRazorAssembly);
            Assert.Single(manager.ApplicationParts.OfType<AssemblyPart>(), p => p.Assembly == mvcTagHelpersAssembly);
        }

        [Fact]
        public void AddMvcTwice_DoesNotAddDuplicateFrameworkParts()
        {
            // Arrange
            var mvcRazorAssembly = typeof(UrlResolutionTagHelper).Assembly;
            var mvcTagHelpersAssembly = typeof(InputTagHelper).Assembly;
            var services = new ServiceCollection();
            var providers = new IApplicationFeatureProvider[]
            {
                new ControllerFeatureProvider(),
                new ViewComponentFeatureProvider()
            };

            // Act
            services.AddMvc();
            services.AddMvc();

            // Assert
            var descriptor = Assert.Single(services, d => d.ServiceType == typeof(ApplicationPartManager));
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
            Assert.NotNull(descriptor.ImplementationInstance);
            var manager = Assert.IsType<ApplicationPartManager>(descriptor.ImplementationInstance);

            Assert.Equal(2, manager.ApplicationParts.Count);
            Assert.Single(manager.ApplicationParts.OfType<AssemblyPart>(), p => p.Assembly == mvcRazorAssembly);
            Assert.Single(manager.ApplicationParts.OfType<AssemblyPart>(), p => p.Assembly == mvcTagHelpersAssembly);
        }

        [Fact]
        public void AddMvcTwice_DoesNotAddApplicationFeatureProvidersTwice()
        {
            // Arrange
            var services = new ServiceCollection();
            var providers = new IApplicationFeatureProvider[]
            {
                new ControllerFeatureProvider(),
                new ViewComponentFeatureProvider()
            };

            // Act
            services.AddMvc();
            services.AddMvc();

            // Assert
            var descriptor = Assert.Single(services, d => d.ServiceType == typeof(ApplicationPartManager));
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
            Assert.NotNull(descriptor.ImplementationInstance);
            var manager = Assert.IsType<ApplicationPartManager>(descriptor.ImplementationInstance);

            Assert.Collection(manager.FeatureProviders,
                feature => Assert.IsType<ControllerFeatureProvider>(feature),
                feature => Assert.IsType<ViewComponentFeatureProvider>(feature),
                feature => Assert.IsType<TagHelperFeatureProvider>(feature),
                feature => Assert.IsType<RazorCompiledItemFeatureProvider>(feature));
        }

        [Fact]
        public void AddMvcCore_ReusesExistingApplicationPartManagerInstance_IfFoundOnServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();
            var manager = new ApplicationPartManager();
            services.AddSingleton(manager);

            // Act
            services.AddMvc();

            // Assert
            var descriptor = Assert.Single(services, d => d.ServiceType == typeof(ApplicationPartManager));
            Assert.Same(manager, descriptor.ImplementationInstance);
        }

        [Fact]
        public void AddMvc_NoScopedServiceIsReferredToByASingleton()
        {
            // Arrange
            var services = new ServiceCollection();

            services.AddSingleton<IWebHostEnvironment>(GetHostingEnvironment());

            var diagnosticListener = new DiagnosticListener("Microsoft.AspNet");
            services.AddSingleton<DiagnosticSource>(diagnosticListener);
            services.AddSingleton<DiagnosticListener>(diagnosticListener);
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddLogging();
            services.AddOptions();
            services.AddMvc();

            var root = services.BuildServiceProvider(validateScopes: true);

            var scopeFactory = root.GetRequiredService<IServiceScopeFactory>();

            // Act & Assert
            using (var scope = scopeFactory.CreateScope())
            {
                foreach (var serviceType in services.Select(d => d.ServiceType).Where(t => !t.IsGenericTypeDefinition).Distinct())
                {
                    // This will throw if something is invalid.
                    scope.ServiceProvider.GetService(typeof(IEnumerable<>).MakeGenericType(serviceType));
                }
            }
        }

        [Fact]
        public void AddMvc_RegistersExpectedTempDataProvider()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMvc();

            // Assert
            var descriptor = Assert.Single(services, item => item.ServiceType == typeof(ITempDataProvider));
            Assert.Equal(typeof(CookieTempDataProvider), descriptor.ImplementationType);
        }

        [Fact]
        public void AddMvc_DoesNotRegisterCookieTempDataOptionsConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var builder = services.AddMvc();

            // Assert
            Assert.DoesNotContain(
                services,
                item => item.ServiceType == typeof(IConfigureOptions<CookieTempDataProviderOptions>));
        }

        private IEnumerable<Type> SingleRegistrationServiceTypes
        {
            get
            {
                var services = new ServiceCollection();
                services.AddSingleton<IWebHostEnvironment>(GetHostingEnvironment());
                services.AddMvc();

                var multiRegistrationServiceTypes = MultiRegistrationServiceTypes;
                return services
                    .Where(sd => !multiRegistrationServiceTypes.Keys.Contains(sd.ServiceType))
                    .Where(sd => sd.ServiceType.Assembly.FullName.Contains("Mvc"))
                    .Select(sd => sd.ServiceType);
            }
        }

        private Dictionary<Type, Type[]> MultiRegistrationServiceTypes
        {
            get
            {
                return new Dictionary<Type, Type[]>()
                {
                    {
                        typeof(IConfigureOptions<MvcOptions>),
                        new Type[]
                        {
                            typeof(MvcCoreMvcOptionsSetup),
                            typeof(MvcDataAnnotationsMvcOptionsSetup),
                            typeof(TempDataMvcOptionsSetup),
                        }
                    },
                    {
                        typeof(IConfigureOptions<RouteOptions>),
                        new Type[]
                        {
                            typeof(MvcCoreRouteOptionsSetup),
                            typeof(MvcCoreRouteOptionsSetup),
                        }
                    },
                    {
                        typeof(IConfigureOptions<ApiBehaviorOptions>),
                        new Type[]
                        {
                            typeof(ApiBehaviorOptionsSetup),
                        }
                    },
                    {
                        typeof(IConfigureOptions<MvcViewOptions>),
                        new Type[]
                        {
                            typeof(MvcViewOptionsSetup),
                            typeof(MvcRazorMvcViewOptionsSetup),
                        }
                    },
                    {
                        typeof(IConfigureOptions<RazorViewEngineOptions>),
                        new[]
                        {
                            typeof(RazorViewEngineOptionsSetup),
                            typeof(RazorPagesRazorViewEngineOptionsSetup),
                        }
                    },
                    {
                        typeof(IPostConfigureOptions<MvcOptions>),
                        new[]
                        {
                            typeof(MvcCoreMvcOptionsSetup),
                        }
                    },
                    {
                        typeof(IActionConstraintProvider),
                        new Type[]
                        {
                            typeof(DefaultActionConstraintProvider),
                        }
                    },
                    {
                        typeof(IActionDescriptorProvider),
                        new Type[]
                        {
                            typeof(ControllerActionDescriptorProvider),
                            typeof(CompiledPageActionDescriptorProvider),
                        }
                    },
                    {
                        typeof(IActionInvokerProvider),
                        new Type[]
                        {
                            typeof(ControllerActionInvokerProvider),
                            typeof(PageActionInvokerProvider),
                        }
                    },
                    {
                        typeof(IRequestDelegateFactory),
                        new Type[]
                        {
                            typeof(PageRequestDelegateFactory),
                            typeof(ControllerRequestDelegateFactory)
                        }
                    },
                    {
                        typeof(IFilterProvider),
                        new Type[]
                        {
                            typeof(DefaultFilterProvider),
                        }
                    },
                    {
                        typeof(IControllerPropertyActivator),
                        new Type[]
                        {
                            typeof(DefaultControllerPropertyActivator),
                            typeof(ViewDataDictionaryControllerPropertyActivator),
                        }
                    },
                    {
                        typeof(IApplicationModelProvider),
                        new Type[]
                        {
                            typeof(DefaultApplicationModelProvider),
                            typeof(CorsApplicationModelProvider),
                            typeof(AuthorizationApplicationModelProvider),
                            typeof(TempDataApplicationModelProvider),
                            typeof(ViewDataAttributeApplicationModelProvider),
                            typeof(ApiBehaviorApplicationModelProvider),
                        }
                    },
                    {
                        typeof(IApiDescriptionProvider),
                        new Type[]
                        {
                            typeof(DefaultApiDescriptionProvider),
                        }
                    },
                    {
                        typeof(IPageRouteModelProvider),
                        new[]
                        {
                            typeof(CompiledPageRouteModelProvider),
                        }
                    },
                    {
                        typeof(IPageApplicationModelProvider),
                        new[]
                        {
                            typeof(AuthorizationPageApplicationModelProvider),
                            typeof(AuthorizationPageApplicationModelProvider),
                            typeof(DefaultPageApplicationModelProvider),
                            typeof(TempDataFilterPageApplicationModelProvider),
                            typeof(ViewDataAttributePageApplicationModelProvider),
                            typeof(ResponseCacheFilterApplicationModelProvider),
                        }
                    },
                };
            }
        }

        private void AssertServiceCountEquals(
            IServiceCollection services,
            Type serviceType,
            int expectedServiceRegistrationCount)
        {
            var serviceDescriptors = services.Where(serviceDescriptor => serviceDescriptor.ServiceType == serviceType);
            var actual = serviceDescriptors.Count();

            Assert.True(
                (expectedServiceRegistrationCount == actual),
                $"Expected service type '{serviceType}' to be registered {expectedServiceRegistrationCount}" +
                $" time(s) but was actually registered {actual} time(s)." + 
                string.Join(Environment.NewLine, serviceDescriptors.Select(sd => sd.ImplementationType)));
        }

        private void AssertContainsSingle(
            IServiceCollection services,
            Type serviceType,
            Type implementationType)
        {
            var matches = services
                .Where(sd =>
                    sd.ServiceType == serviceType &&
                    sd.ImplementationType == implementationType)
                .ToArray();

            if (matches.Length == 0)
            {
                Assert.True(
                    false,
                    $"Could not find an instance of {implementationType} registered as {serviceType}");
            }
            else if (matches.Length > 1)
            {
                Assert.True(
                    false,
                    $"Found multiple instances of {implementationType} registered as {serviceType}");
            }
        }

        private IWebHostEnvironment GetHostingEnvironment()
        {
            var environment = new Mock<IWebHostEnvironment>();
            environment
                .Setup(e => e.ApplicationName)
                .Returns(typeof(MvcServiceCollectionExtensionsTest).Assembly.GetName().Name);

            return environment.Object;
        }
    }
}
