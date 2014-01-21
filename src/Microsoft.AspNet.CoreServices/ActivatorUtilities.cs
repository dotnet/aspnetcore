using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNet.CoreServices
{
    /// <summary>
    /// Helper code for the various activator services.
    /// </summary>
    public static class ActivatorUtilities
    {
        /// <summary>
        /// Retrieve an instance of the given type from the service provider. If one is not found then instantiate it directly.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object GetServiceOrCreateInstance(IServiceProvider services, Type type)
        {
            return GetServiceNoExceptions(services, type) ?? CreateInstance(services, type);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "IServiceProvider may throw unknown exceptions")]
        private static object GetServiceNoExceptions(IServiceProvider services, Type type)
        {
            try
            {
                return services.GetService(type);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Instantiate an object of the given type, using constructor service injection if possible.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object CreateInstance(IServiceProvider services, Type type)
        {
            return CreateFactory(type).Invoke(services);
        }

        /// <summary>
        /// Creates a factory to instantiate a type using constructor service injection if possible.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Func<IServiceProvider, object> CreateFactory(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            ConstructorInfo[] constructors = type.GetTypeInfo()
                .DeclaredConstructors
                .Where(IsInjectable)
                .ToArray();

            if (constructors.Length == 1)
            {
                ParameterInfo[] parameters = constructors[0].GetParameters();
                return services =>
                {
                    var args = new object[parameters.Length];
                    for (int index = 0; index != parameters.Length; ++index)
                    {
                        args[index] = services.GetService(parameters[index].ParameterType);
                    }
                    return Activator.CreateInstance(type, args);
                };
            }
            return _ => Activator.CreateInstance(type);
        }

        private static bool IsInjectable(ConstructorInfo constructor)
        {
            return constructor.IsPublic && constructor.GetParameters().Length != 0;
        }

        public static Func<TBase> Create<TBase>(Type instanceType) where TBase : class
        {
            Contract.Assert(instanceType != null);
            NewExpression newInstanceExpression = Expression.New(instanceType);
            return Expression.Lambda<Func<TBase>>(newInstanceExpression).Compile();
        }

        public static Func<TInstance> Create<TInstance>() where TInstance : class
        {
            return Create<TInstance>(typeof(TInstance));
        }

        public static Func<object> Create(Type instanceType)
        {
            Contract.Assert(instanceType != null);
            return Create<object>(instanceType);
        }
    }
}
