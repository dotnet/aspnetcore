# Microsoft.Extensions.DependencyInjection.Extensions

``` diff
 namespace Microsoft.Extensions.DependencyInjection.Extensions {
     public static class ServiceCollectionDescriptorExtensions {
         public static IServiceCollection Add(this IServiceCollection collection, ServiceDescriptor descriptor);
         public static IServiceCollection Add(this IServiceCollection collection, IEnumerable<ServiceDescriptor> descriptors);
         public static IServiceCollection RemoveAll(this IServiceCollection collection, Type serviceType);
         public static IServiceCollection RemoveAll<T>(this IServiceCollection collection);
         public static IServiceCollection Replace(this IServiceCollection collection, ServiceDescriptor descriptor);
         public static void TryAdd(this IServiceCollection collection, ServiceDescriptor descriptor);
         public static void TryAdd(this IServiceCollection collection, IEnumerable<ServiceDescriptor> descriptors);
         public static void TryAddEnumerable(this IServiceCollection services, ServiceDescriptor descriptor);
         public static void TryAddEnumerable(this IServiceCollection services, IEnumerable<ServiceDescriptor> descriptors);
         public static void TryAddScoped(this IServiceCollection collection, Type service);
         public static void TryAddScoped(this IServiceCollection collection, Type service, Func<IServiceProvider, object> implementationFactory);
         public static void TryAddScoped(this IServiceCollection collection, Type service, Type implementationType);
         public static void TryAddScoped<TService, TImplementation>(this IServiceCollection collection) where TService : class where TImplementation : class, TService;
         public static void TryAddScoped<TService>(this IServiceCollection collection) where TService : class;
         public static void TryAddScoped<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory) where TService : class;
         public static void TryAddSingleton(this IServiceCollection collection, Type service);
         public static void TryAddSingleton(this IServiceCollection collection, Type service, Func<IServiceProvider, object> implementationFactory);
         public static void TryAddSingleton(this IServiceCollection collection, Type service, Type implementationType);
         public static void TryAddSingleton<TService, TImplementation>(this IServiceCollection collection) where TService : class where TImplementation : class, TService;
         public static void TryAddSingleton<TService>(this IServiceCollection collection) where TService : class;
         public static void TryAddSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory) where TService : class;
         public static void TryAddSingleton<TService>(this IServiceCollection collection, TService instance) where TService : class;
         public static void TryAddTransient(this IServiceCollection collection, Type service);
         public static void TryAddTransient(this IServiceCollection collection, Type service, Func<IServiceProvider, object> implementationFactory);
         public static void TryAddTransient(this IServiceCollection collection, Type service, Type implementationType);
         public static void TryAddTransient<TService, TImplementation>(this IServiceCollection collection) where TService : class where TImplementation : class, TService;
         public static void TryAddTransient<TService>(this IServiceCollection collection) where TService : class;
         public static void TryAddTransient<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory) where TService : class;
     }
 }
```

