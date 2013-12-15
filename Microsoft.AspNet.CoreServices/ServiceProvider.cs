using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.CoreServices
{
    /// <summary>
    /// The default IServiceProvider.
    /// </summary>
    public class ServiceProvider : IServiceProvider
    {
        private readonly IDictionary<Type, Func<object>> _services = new Dictionary<Type, Func<object>>();
        private readonly IDictionary<Type, List<Func<object>>> _priorServices = new Dictionary<Type, List<Func<object>>>();

        /// <summary>
        /// 
        /// </summary>
        public ServiceProvider()
        {
            _services[typeof(IServiceProvider)] = () => this;
        }

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public virtual object GetService(Type serviceType)
        {
            return GetSingleService(serviceType) ?? GetMultiService(serviceType);
        }

        private object GetSingleService(Type serviceType)
        {
            Func<object> serviceFactory;
            return _services.TryGetValue(serviceType, out serviceFactory)
                ? serviceFactory.Invoke()
                : null;
        }

        private object GetMultiService(Type collectionType)
        {
            if (collectionType.GetTypeInfo().IsGenericType &&
                collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                Type serviceType = collectionType.GetTypeInfo().GenericTypeArguments.Single();
                Type listType = typeof(List<>).MakeGenericType(serviceType);
                var services = (IList)Activator.CreateInstance(listType);

                Func<object> serviceFactory;
                if (_services.TryGetValue(serviceType, out serviceFactory))
                {
                    services.Add(serviceFactory());

                    List<Func<object>> prior;
                    if (_priorServices.TryGetValue(serviceType, out prior))
                    {
                        foreach (var factory in prior)
                        {
                            services.Add(factory());
                        }
                    }
                }
                return services;
            }
            return null;
        }

        /// <summary>
        /// Remove all occurrences of the given type from the provider.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual ServiceProvider RemoveAll<T>()
        {
            return RemoveAll(typeof(T));
        }

        /// <summary>
        /// Remove all occurrences of the given type from the provider.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual ServiceProvider RemoveAll(Type type)
        {
            _services.Remove(type);
            _priorServices.Remove(type);
            return this;
        }

        /// <summary>
        /// Add an instance of type TService to the list of providers.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="instance"></param>
        /// <returns></returns>
        public virtual ServiceProvider AddInstance<TService>(object instance)
        {
            return AddInstance(typeof(TService), instance);
        }

        /// <summary>
        /// Add an instance of the given type to the list of providers.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public virtual ServiceProvider AddInstance(Type service, object instance)
        {
            return Add(service, () => instance);
        }

        /// <summary>
        /// Specify that services of the type TService should be fulfilled by the type TImplementation.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <returns></returns>
        public virtual ServiceProvider Add<TService, TImplementation>()
        {
            return Add(typeof(TService), typeof(TImplementation));
        }

        /// <summary>
        /// Specify that services of the type serviceType should be fulfilled by the type implementationType.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <returns></returns>
        public virtual ServiceProvider Add(Type serviceType, Type implementationType)
        {
            Func<IServiceProvider, object> factory = ActivatorUtilities.CreateFactory(implementationType);
            return Add(serviceType, () => factory(this));
        }

        /// <summary>
        /// Specify that services of the given type should be created with the given serviceFactory.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="serviceFactory"></param>
        /// <returns></returns>
        public virtual ServiceProvider Add(Type serviceType, Func<object> serviceFactory)
        {
            Func<object> existing;
            if (_services.TryGetValue(serviceType, out existing))
            {
                List<Func<object>> prior;
                if (_priorServices.TryGetValue(serviceType, out prior))
                {
                    prior.Add(existing);
                }
                else
                {
                    prior = new List<Func<object>> { existing };
                    _priorServices.Add(serviceType, prior);
                }
            }
            _services[serviceType] = serviceFactory;
            return this;
        }
    }
}
